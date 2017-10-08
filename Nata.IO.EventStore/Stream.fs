namespace Nata.IO.EventStore

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Threading
open NLog.FSharp
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
    type Direction = Forward | Reverse

    let private log = new Logger()

    let private decode (resolvedEvent:ResolvedEvent) =
        Event.create           resolvedEvent.Event.Data
        |> Event.withCreatedAt resolvedEvent.Event.Created
        |> Event.withStream    resolvedEvent.Event.EventStreamId
        |> Event.withEventType resolvedEvent.Event.EventType
        |> Event.withBytes     resolvedEvent.Event.Metadata
        |> Event.withIndex    (resolvedEvent.Event.EventNumber |> int64),
        resolvedEvent.Event.EventNumber

    let rec index (connection : IEventStoreConnection)
                  (stream : string)
                  (size : int)
                  (direction : Direction)
                  (position : Position<Index>) : Index =
        match position with
        | Position.At x -> x
        | Position.Before x -> (index connection stream size direction x) - 1L
        | Position.After x -> (index connection stream size direction x) + 1L
        | Position.Start -> (int64 StreamPosition.Start)
        | Position.End when direction = Direction.Forward ->
            read connection stream size Direction.Reverse position
            |> Seq.tryPick Some
            |> Option.map (snd >> (+) 1L)
            |> Option.getValueOr (int64 StreamPosition.Start)
        | Position.End -> (int64 StreamPosition.End)


    and private read (connection : IEventStoreConnection)
                     (stream : string)
                     (size : int)
                     (direction : Direction)
                     (position : Position<Index>) : seq<Event*Index> =
        seq {
            let from = index connection stream size direction position
            let sliceTask = 
                match direction with
                | Direction.Forward -> connection.ReadStreamEventsForwardAsync(stream, from, size, true)
                | Direction.Reverse -> connection.ReadStreamEventsBackwardAsync(stream, from, size, true)
            let slice = sliceTask |> Async.AwaitTask |> Async.RunSynchronously
            match slice.Status with
            | SliceReadStatus.StreamDeleted -> 
                log.Warn "Stream %s at (%d) was deleted." stream from
            | SliceReadStatus.StreamNotFound -> 
                log.Warn "Stream %s at (%d) was not found." stream from
            | SliceReadStatus.Success ->
                if slice.Events.Length > 0 then
                    for resolvedEvent in slice.Events ->
                        decode resolvedEvent
                    yield! read connection stream size direction (Position.At slice.NextEventNumber)
            | x -> failwith (sprintf "Stream %s at (%d) produced undocumented response: %A" stream from x)
        }


    and private listen (connection : IEventStoreConnection)
                       (stream : string)
                       (size : int)
                       (position : Position<Index>) : seq<Event*Index> =
        let from = Math.Max(0L, index connection stream size Direction.Forward position)
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


    let private write (connection : IEventStoreConnection)
                      (targetStream : string)
                      (position : Position<Index>)
                      (event : Event) : Index =
        let rec targetVersionOf = function
            | Position.Start -> int64 ExpectedVersion.EmptyStream
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
        let eventMetadata = Event.bytes event |> Option.getValueOr [||]
        let eventType = Event.eventType event |> Option.getValueOrYield guid
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
            read connection stream 1 Direction.Reverse Position.End 
            |> Seq.tryPick Some
        let update(event,index) =
            let position =
                index
                |> Option.map ((+) 1L >> Position.At)
                |> Option.getValueOr (Position.Start)
            try write connection stream position event |> Some
            with :? Position.Invalid<Index> -> None
        let rec apply(last:(Event*Index) option) =
            seq {
                let eventIn, indexIn =
                    last 
                    |> Option.coalesceYield state
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

    let connect : Connector<Settings,Name,Data,Index> =
        fun settings ->
            let connection = Client.connect settings
            let size = settings.Options.BatchSize
            fun stream ->
            [   
                Capability.Indexer <|
                    index connection stream size Direction.Forward

                Capability.Reader <| fun () ->
                    read connection stream size Direction.Forward Position.Start |> Seq.map fst

                Capability.ReaderFrom <|
                    read connection stream size Direction.Forward

                Capability.Writer <| fun event ->
                    write connection stream Position.End event |> ignore

                Capability.WriterTo <|
                    write connection stream

                Capability.Subscriber <| fun () ->
                    listen connection stream size Position.Start |> Seq.map fst

                Capability.SubscriberFrom <|
                    listen connection stream size

                Capability.Competitor <| fun fn ->
                    compete connection stream fn
            ]