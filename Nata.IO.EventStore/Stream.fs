namespace Nata.IO.EventStore

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Threading
open NLog.FSharp
open EventStore.ClientAPI
open EventStore.ClientAPI.Exceptions
open EventStore.ClientAPI.SystemData

module Stream =

    type Data = byte[]
    type Event = Nata.IO.Event<Data>
    type Name = string
    type Index = int

    let private decode (resolvedEvent:ResolvedEvent) =
        Nata.IO.Event.create           resolvedEvent.Event.Data
        |> Nata.IO.Event.withCreatedAt resolvedEvent.Event.Created
        |> Nata.IO.Event.withStream    resolvedEvent.Event.EventStreamId
        |> Nata.IO.Event.withEventType resolvedEvent.Event.EventType
        |> Nata.IO.Event.withBytes     resolvedEvent.Event.Metadata,
        resolvedEvent.Event.EventNumber


    let rec private read (connection : IEventStoreConnection)
                         (stream : string)
                         (from : int) : seq<Event*Index> =
        seq {
            let sliceTask = connection.ReadStreamEventsForwardAsync(stream, from, 1000, true)
            let slice = sliceTask |> Async.AwaitTask |> Async.RunSynchronously
            match slice.Status with
            | SliceReadStatus.StreamDeleted -> failwith (sprintf "Stream %s at (%d) was deleted." stream from)
            | SliceReadStatus.StreamNotFound -> failwith (sprintf "Stream %s at (%d) was not found." stream from)
            | SliceReadStatus.Success ->
                if slice.Events.Length > 0 then
                    for resolvedEvent in slice.Events ->
                        decode resolvedEvent
                    yield! read connection stream slice.NextEventNumber
            | x -> failwith (sprintf "Stream %s at (%d) produced undocumented response: %A" stream from x)
        }


    let rec private listen (connection : IEventStoreConnection)
                           (stream : string)
                           (from : int) : seq<Event*Index> =
        let queue = new BlockingCollection<Option<Event*Index>>(new ConcurrentQueue<Option<Event*Index>>())
        let subscription =
            let start = match from-1 with -1 -> Nullable() | x -> Nullable(x)
            let onDropped,onEvent,onLive =
                Action<_,_,_>(fun _ _ _ -> queue.Add None),
                Action<_,_>(fun _ -> decode >> Some >> queue.Add),
                Action<_>(ignore)
            connection.SubscribeToStreamFrom(stream, start, true, onEvent, onLive, onDropped)

        let rec traverse last =
            seq {
                match queue.Take() with
                | Some (event, index) ->
                    yield event, index
                    yield! traverse (index+1)
                | None ->
                    Thread.Sleep(10000)
                    yield! listen connection stream last
            }
        traverse from


    let private write (connection : IEventStoreConnection)
                      (targetStream : string)
                      (targetVersion : int)
                      (event : Event) : Index =
        let eventId = Guid.NewGuid()
        let eventPosition =
            match targetVersion with
            | x when x < 0 -> ExpectedVersion.Any
            | version -> version
        let eventMetadata =
            match Nata.IO.Event.bytes event with
            | Some x -> x
            | None -> [||]
        let eventType =
            match Nata.IO.Event.eventType event with
            | Some x -> x
            | None -> Guid.NewGuid().ToString("n")
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
                    raise (Nata.IO.InvalidPosition(Nata.IO.Position.At targetVersion))
                | _ ->
                    raise exn
            | _ -> raise exn
        

    let connect : Nata.IO.Connector<Settings,Name,Data,Index> =
        Client.connect >> (fun connection stream ->
            [   
                Nata.IO.Capability.Reader <| fun () ->
                    read connection stream 0 |> Seq.map fst

                Nata.IO.Capability.ReaderFrom <|
                    read connection stream

                Nata.IO.Capability.Writer <| fun event ->
                    write connection stream ExpectedVersion.Any event |> ignore

                Nata.IO.Capability.WriterTo <|
                    write connection stream

                Nata.IO.Capability.Subscriber <| fun () ->
                    listen connection stream 0 |> Seq.map fst

                Nata.IO.Capability.SubscriberFrom <|
                    listen connection stream
            ])