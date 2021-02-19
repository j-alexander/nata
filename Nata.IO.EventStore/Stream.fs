namespace Nata.IO.EventStore

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Threading
open NLog
open Nata.Core
open Nata.IO
open EventStore.ClientAPI
open EventStore.ClientAPI.Exceptions
open EventStore.ClientAPI.SystemData

module Stream =

    type Data = byte[]
    type Event = Event<Data>
    type Name = string
    type Index = int64
    type Position = Position<Index>

    module Traversal =

        let log = LogManager.GetLogger("Nata.IO.EventStore.Stream")

        let decode (resolvedEvent:ResolvedEvent) =
            Event.create           resolvedEvent.Event.Data
            |> Event.withCreatedAt resolvedEvent.Event.Created
            |> Event.withStream    resolvedEvent.Event.EventStreamId
            |> Event.withEventType resolvedEvent.Event.EventType
            |> Event.withBytes     resolvedEvent.Event.Metadata
            |> Event.withIndex    (resolvedEvent.Event.EventNumber |> int64),
            resolvedEvent.Event.EventNumber

        let last (connection : IEventStoreConnection)
                 (stream : string) =
            let slice =
                connection.ReadStreamEventsBackwardAsync(stream, int64 StreamPosition.End, 1, true)
                |> Async.AwaitTask
                |> Async.RunSynchronously
            match slice.Status with
            | SliceReadStatus.StreamDeleted -> 
                log.Warn(sprintf "Stream %s was deleted." stream)
                None
            | SliceReadStatus.StreamNotFound -> 
                log.Warn(sprintf "Stream %s was not found." stream)
                None
            | SliceReadStatus.Success ->
                slice.Events
                |> Seq.map decode
                |> Seq.tryLast
            | x -> failwith (sprintf "Stream %s produced undocumented response: %A" stream x)

        let index (connection : IEventStoreConnection)
                  (stream : string) :  Position<Index> -> Index =
            let rec net (offset : Index) =
                function
                | Position.At x -> offset + x
                | Position.Before x -> net (offset - 1L) x
                | Position.After x -> net (offset + 1L) x
                | Position.Start when offset < 0L -> int64 StreamPosition.Start
                | Position.Start -> offset
                | Position.End ->
                    let last =
                        last connection stream
                        |> Option.map (snd >> (+) 1L)
                        |> Option.defaultValue (int64 StreamPosition.Start)
                    Math.Max(int64 StreamPosition.Start, last + offset)
            net 0L

        let rec read (connection : IEventStoreConnection)
                     (stream : string)
                     (size : int)
                     (position : Position<Index>) : seq<Event*Index> =
            seq {
                let from = index connection stream position
                let slice =
                    connection.ReadStreamEventsForwardAsync(stream, from, size, true)
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                match slice.Status with
                | SliceReadStatus.StreamDeleted -> 
                    log.Warn(sprintf "Stream %s at (%d) was deleted." stream from)
                | SliceReadStatus.StreamNotFound -> 
                    log.Warn(sprintf "Stream %s at (%d) was not found." stream from)
                | SliceReadStatus.Success ->
                    if slice.Events.Length > 0 then
                        for resolvedEvent in slice.Events ->
                            decode resolvedEvent
                        yield! read connection stream size (Position.At slice.NextEventNumber)
                | x -> failwith (sprintf "Stream %s at (%d) produced undocumented response: %A" stream from x)
            }

        let rec listen (connection : IEventStoreConnection)
                       (stream : string)
                       (size : int)
                       (position : Position<Index>) : seq<Event*Index> =
            let from = index connection stream position
            let queue = new BlockingCollection<Option<Event*Index>>(new ConcurrentQueue<Option<Event*Index>>())
            let subscription =
                let settings = CatchUpSubscriptionSettings.Default
                let onDropped,onEvent,onLive =
                    Action<_,_,_>(fun _ _ _ -> queue.Add None),
                    Action<_,_>(fun _ -> decode >> Some >> queue.Add),
                    Action<_>(ignore)
                match from with
                | 0L ->
                    connection.SubscribeToStreamFrom(stream, Nullable(), settings, onEvent, onLive, onDropped)
                    |> Client.Catchup
                | x ->
                    connection.SubscribeToStreamFrom(stream, Nullable(x-1L), settings, onEvent, onLive, onDropped)
                    |> Client.Catchup

            let rec traverse last =
                seq {
                    match queue.Take() with
                    | Some (event, index) ->
                        yield event, index
                        yield! traverse (index+1L)
                    | None ->
                        Thread.Sleep(10000)
                        yield! listen connection stream size (Position.At last)
                }
            traverse from

        let write (connection : IEventStoreConnection)
                  (targetStream : string)
                  (position : Position<Index>)
                  (event : Event) : Index =
            let rec targetVersionOf = function
                | Position.Start -> int64 ExpectedVersion.NoStream
                | Position.At x -> x-1L
                | Position.End -> int64 ExpectedVersion.Any  
                | Position.Before x ->
                    match targetVersionOf x with
                    | index when index < 0L -> raise (Position.Invalid(position))
                    | index -> index - 1L
                | Position.After x ->
                    match targetVersionOf x with
                    | index when index < -1L -> raise (Position.Invalid(position))
                    | index -> index + 1L
            let eventId = Guid.NewGuid()
            let eventPosition = targetVersionOf position
            let eventMetadata = Event.bytes event |> Option.defaultValue [||]
            let eventType = Event.eventType event |> Option.defaultWith guid
            let eventData = new EventData(eventId, eventType, true, event.Data, eventMetadata)
            let result =
                connection.AppendToStreamAsync(targetStream, eventPosition, eventData)
                |> Async.AwaitTask
                |> Async.Catch
                |> Async.RunSynchronously
            match result with
            | Choice1Of2 result -> result.NextExpectedVersion
            | Choice2Of2 exn ->
                match exn with
                | :? AggregateException as e ->
                    match e.InnerException with
                    | :? WrongExpectedVersionException as v ->
                        raise (Position.Invalid(position))
                    | _ ->
                        raise exn
                | _ -> raise exn

        let compete (connection : IEventStoreConnection)
                            (stream : string)
                            (fn : Event<Data> option->Event<Data>) =
            let state() =
                last connection stream
            let update(event,index) =
                let position =
                    index
                    |> Option.map ((+) 1L >> Position.At)
                    |> Option.defaultValue (Position.Start)
                try write connection stream position event |> Some
                with :? Position.Invalid<Index> -> None
            let rec apply(last:(Event*Index) option) =
                seq {
                    let eventIn, indexIn =
                        last 
                        |> Option.coalesceWith state
                        |> Option.distribute
                    let eventOut = fn eventIn
                    let result = 
                        update(eventOut, indexIn)
                        |> Option.map (fun indexOut -> eventOut, indexOut)
                    match result with
                    | None -> ()
                    | Some _ ->
                        yield eventOut
                    yield! apply result
                }
            apply(None)

    let connectWithMetadata : Connector<Settings,Name*Metadata,Data,Index> =
        fun settings ->
            let connection = Client.connect settings
            let size = settings.Options.BatchSize
            Metadata.set connection
            >>
            fun stream ->
            [   
                Capability.Indexer <|
                    Traversal.index connection stream

                Capability.Reader <| fun () ->
                    Traversal.read connection stream size Position.Start |> Seq.map fst

                Capability.ReaderFrom <|
                    Traversal.read connection stream size

                Capability.Writer <| fun event ->
                    Traversal.write connection stream Position.End event |> ignore

                Capability.WriterTo <|
                    Traversal.write connection stream

                Capability.Subscriber <| fun () ->
                    Traversal.listen connection stream size Position.Start |> Seq.map fst

                Capability.SubscriberFrom <|
                    Traversal.listen connection stream size

                Capability.Competitor <| fun fn ->
                    Traversal.compete connection stream fn
            ]

    let connect : Connector<Settings,Name,Data,Index> =
        connectWithMetadata 
        >> fun source name -> source(name, Metadata.empty)