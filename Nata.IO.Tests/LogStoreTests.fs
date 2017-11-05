namespace Nata.IO.Tests

open System
open System.Collections.Concurrent
open System.Reflection
open System.Text
open System.Threading
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Channel

[<AbstractClass>]
type LogStoreTests() as x =

    let event(fn) =
        [| 
            "case", JsonValue.String (x.GetType().Name + "." + fn)
            "at", JsonValue.String (DateTime.Now.ToString()) 
            "from", JsonValue.String (Assembly.GetExecutingAssembly().FullName)
        |]
        |> JsonValue.Record
        |> JsonValue.toBytes       
        |> Event.create
        |> Event.withEventType fn

    abstract member Connect : unit -> Channel<byte[],int64>

    [<Test>]
    member x.TestConnect() =
        let capabilities = x.Connect()
        Assert.Greater(capabilities.Length, 0)

    [<Test>]
    member x.TestWrite() =
        let write = x.Connect() |> writer
        let event = event("TestWrite")
        write event

    [<Test; Timeout(45000)>]
    member x.TestEmptyRead() =
        let read = x.Connect() |> reader
        Assert.AreEqual([], read() |> Seq.toList)

    [<Test>]
    member x.TestRead() =
        let connection = x.Connect()
        let write = writer connection
        let read = reader connection
        let event = event("TestRead")
        write event

        let result = read() |> Seq.head
        Assert.AreEqual(event.Data, result.Data)

    [<Test; Timeout(45000)>]
    member x.TestEmptyReadFrom() =
        let readFrom = x.Connect() |> readerFrom
        Assert.AreEqual([], readFrom Position.Start |> Seq.toList)

    [<Test>]
    member x.TestReadFrom() =
        let connection = x.Connect()
        let write = writer connection
        let readFrom = readerFrom connection
        let event_0 = event("TestReadFrom-0")
        let event_1 = event("TestReadFrom-1")
        let event_2 = event("TestReadFrom-2")
        write event_0
        write event_1
        write event_2
        
        let result, index_0 = readFrom (Position.Start) |> Seq.head
        Assert.GreaterOrEqual(index_0, 0L)
        Assert.AreEqual(event_0.Data, result.Data)
        
        let result, index_1 = readFrom (Position.At (1L+index_0)) |> Seq.head
        Assert.Greater(index_1, index_0)
        Assert.AreEqual(event_1.Data, result.Data)
        
        let result, index_2 = readFrom (Position.At (1L+index_1)) |> Seq.head
        Assert.Greater(index_2, index_1)
        Assert.AreEqual(event_2.Data, result.Data)

        let result, index = readFrom (Position.Start) |> Seq.head
        Assert.AreEqual(event_0.Data, result.Data)

    [<Test>]
    member x.TestWriteTo() =
        let connection = x.Connect()
        match tryWriterTo connection with
        | Some writeTo ->
            [ 0L, event "TestWriteTo-0" |> writeTo (Position.At 0L)
              1L, event "TestWriteTo-1" |> writeTo (Position.At 1L)
              2L, event "TestWriteTo-2" |> writeTo (Position.At 2L) ]
            |> List.iter Assert.AreEqual
        | _ ->
            Assert.Ignore("WriterTo is reported to be unsupported by this source.")

    [<Test>]
    member x.TestWriteToPosition() =
        let connection = x.Connect()
        match tryWriterTo connection, tryReaderFrom connection with
        | Some writeTo, Some readFrom ->
            let event_0, event_1, event_2 =
                event "TestWriteTo-0",
                event "TestWriteTo-1",
                event "TestWriteTo-2"
            [ 0L, event_0 |> writeTo Position.Start
              1L, event_1 |> writeTo Position.End
              2L, event_2 |> writeTo Position.End ]
            |> List.iter Assert.AreEqual
            match readFrom Position.Start |> Seq.toList with
            | [ result_0, 0L ; result_1, 1L ; result_2, 2L ] ->
                Assert.AreEqual(event_0.Data, result_0.Data)
                Assert.AreEqual(event_1.Data, result_1.Data)
                Assert.AreEqual(event_2.Data, result_2.Data)
            | _ -> Assert.Fail()
        | _ ->
            Assert.Ignore("ReaderFrom and WriterTo are reported to be unsupported by this source.")

    [<Test>]
    member x.TestWriteToShouldFailWithIndexTooLow() =
        let connection = x.Connect()
        match tryWriterTo connection with
        | Some writeTo ->
            event "TestWriteToShouldFailWithIndexTooLow-0" |> writeTo (Position.At -1L) |> ignore
            Assert.Throws<Position.Invalid<int64>>(fun _ ->
                event "TestWriteToShouldFailWithIndexTooLow-1" |> writeTo (Position.At 0L) |> ignore
            ) |> ignore
            Assert.Throws<Position.Invalid<int64>>(fun _ ->
                event "TestWriteToShouldFailWithIndexTooLow-2" |> writeTo (Position.At 0L) |> ignore
            ) |> ignore
        | _ ->
            Assert.Ignore("WriterTo is reported to be unsupported by this source.")

    [<Test>]
    member x.TestWriteToShouldFailWithIndexTooHigh() =
        let connection = x.Connect()
        match tryWriterTo connection with
        | Some writeTo ->
            event "TestWriteToShouldFailWithIndexTooHigh-0" |> writeTo (Position.At -1L) |> ignore
            Assert.Throws<Position.Invalid<int64>>(fun _ ->
                event "TestWriteToShouldFailWithIndexTooHigh-1" |> writeTo (Position.At 0L) |> ignore
            ) |> ignore
            Assert.Throws<Position.Invalid<int64>>(fun _ ->
                event "TestWriteToShouldFailWithIndexTooHigh-2" |> writeTo (Position.At 2L) |> ignore
            ) |> ignore
        | _ ->
            Assert.Ignore("WriterTo is reported to be unsupported by this source.")
        
    [<Test; Timeout(120000)>]
    member x.TestLiveSubscription() =
        let write, subscribe =
            let connection = x.Connect()
            writer connection, subscriber connection
        let results = subscribe()
        let expected =
            [ event "TestLiveSubscription-0"
              event "TestLiveSubscription-1"
              event "TestLiveSubscription-2" ]
        for event in expected do
            write event
        results
        |> Seq.take 3
        |> Seq.toList
        |> List.zip expected
        |> List.iter(fun (expected, actual) ->
            Assert.AreEqual(expected.Data, actual.Data))
            
    [<Test; Timeout(120000)>]
    member x.TestLateSubscription() =
        let write, subscribe =
            let connection = x.Connect()
            writer connection, subscriber connection
        let expected =
            [ event "TestLateSubscription-0"
              event "TestLateSubscription-1"
              event "TestLateSubscription-2" ]
        for event in expected do
            write event
        subscribe()
        |> Seq.take 3
        |> Seq.toList
        |> List.zip expected
        |> List.iter(fun (expected, actual) ->
            Assert.AreEqual(expected.Data, actual.Data))
            
    [<Test; Timeout(120000)>]
    member x.TestSubscriptionFromIndex() =
        let write, subscribeFrom =
            let connection = x.Connect()
            writer connection, subscriberFrom connection
        let expected =
            [| event "TestSubscriptionFromIndex-0"
               event "TestSubscriptionFromIndex-1"
               event "TestSubscriptionFromIndex-2" |]
        for event in expected do
            write event
        let indexes =
            subscribeFrom (Position.Start)
            |> Seq.take 3
            |> Seq.map snd
            |> Seq.toList
        subscribeFrom (Position.Start)
        |> Seq.take 3
        |> Seq.toArray
        |> Array.zip expected
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At (indexes.[0]))
        |> Seq.take 3
        |> Seq.toArray
        |> Array.zip expected
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At (indexes.[1]))
        |> Seq.take 2
        |> Seq.toArray
        |> Array.zip (expected.[1..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At (indexes.[2]))
        |> Seq.take 1
        |> Seq.toArray
        |> Array.zip (expected.[2..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
            
    [<Test; Timeout(120000)>]
    member x.TestSubscriptionContinuation() =
        let write, subscribeFrom =
            let connection = x.Connect()
            writer connection,
            subscriberFrom connection

        let flag,stop =
            event "TestSubscriptionContinuation-flag",
            event "TestSubscriptionContinuation-stop"
        let results = new BlockingCollection<byte[]>()
        let subscriber =
            async {
                subscribeFrom Position.End
                |> Seq.takeWhile (fst >> Event.data >> (=) (Event.data flag))
                |> Seq.iter (fst >> Event.data >> results.Add)
            } |> Async.StartAsTask
              |> Async.AwaitTask

        let rec publish() =
            async {
                do! Async.Sleep(1000)
                match results |> Seq.tryFind ((=) (Event.data flag)) with
                | None ->
                    write flag
                    return! publish()
                | Some x ->
                    write stop
                    return ()
            }
            
        publish()
        |> Async.RunSynchronously
        subscriber
        |> Async.RunSynchronously

    [<Test>]
    member x.TestIndexEmpty() =
        match x.Connect() |> tryIndexer with
        | None ->
            Assert.Ignore("Indexer is reported to be unsupported by this source.")
        | Some index ->
            let tail = index Position.End
            Assert.AreEqual(tail, index Position.Start)
            Assert.AreEqual(tail, index Position.End)

    [<Test>]
    member x.TestIndexWithWrites() =
        let tryIndex, tryWrite =
            let connection = x.Connect()
            tryIndexer connection,
            tryWriter connection
        match tryIndex, tryWrite with
        | Some index, Some write ->
            let tail = index Position.End
            Assert.AreEqual(tail, index Position.Start)
            Assert.AreEqual(tail, index Position.End)
            write(event("TestIndexWithWrites-0"))
            Assert.AreEqual(tail, index Position.Start)
            Assert.AreEqual(tail+1L, index Position.End)
            write(event("TestIndexWithWrites-1"))
            Assert.AreEqual(tail, index Position.Start)
            Assert.AreEqual(tail+2L, index Position.End)
            write(event("TestIndexWithWrites-2"))
            Assert.AreEqual(tail, index Position.Start)
            Assert.AreEqual(tail+3L, index Position.End)
        | _ ->
            Assert.Ignore("Indexer or Writer is reported to be unsupported by this source.")

    [<Test>]
    member x.TestLoneCompetitor() =
        let tryCompetitor, tryReaderFrom, tryWriter =
            let connection =
                let codec =
                    Codec.BytesToString
                    |> Codec.concatenate Codec.StringToInt32
                x.Connect()
                |> Channel.mapData codec
            tryCompetitor connection,
            tryReaderFrom connection,
            tryWriter connection
        match tryCompetitor, tryReaderFrom, tryWriter with
        | Some compete, Some readFrom, Some write ->
            let generation : int list =
                compete (Option.defaultValue (Event.create 2) >> Event.map ((*) 2))
                |> Seq.take 10
                |> Seq.map Event.data
                |> Seq.toList
            let expectation : int list =
                [ 4; 8; 16; 32; 64; 128; 256; 512; 1024; 2048 ]
            Assert.AreEqual(expectation, generation)
            let verification =
                readFrom Position.Start
                |> Seq.take 10
                |> Seq.map (fst >> Event.data)
                |> Seq.toList
            Assert.AreEqual(expectation, verification)
        | _ ->
            Assert.Ignore("Competitor, ReaderFrom or Writer is reported to be unsupported by this source.")

    [<Test>]
    member x.TestTwoCompetitors() =
        let tryCompetitor, tryReaderFrom, tryWriter =
            let connection =
                let codec =
                    Codec.BytesToString
                    |> Codec.concatenate Codec.StringToInt32
                x.Connect()
                |> Channel.mapData codec
            tryCompetitor connection,
            tryReaderFrom connection,
            tryWriter connection
        match tryCompetitor, tryReaderFrom, tryWriter with
        | Some compete, Some readFrom, Some write ->
            let generation (delay:int->int) : int seq =
                compete (Option.defaultValue (Event.create 2) >> fun e ->
                    let input = Event.data e
                    let output = input * 2
                    Thread.Sleep(delay input)
                    Event.create output)
                |> Seq.take 11
                |> Seq.map Event.data

            let results =
                let getsSlower, getsFaster =
                    (fun i -> 2 * i),
                    (fun i -> Math.Max(1, 2048/i))
                Seq.consume
                    [ 
                      generation getsSlower
                      //|> Seq.log (printfn "The early bird gets the worm:%d")
                      generation getsFaster
                      //|> Seq.log (printfn "The second mouse gets the cheese:%d")
                    ]
                |> Seq.take 11
                |> Seq.toList

            let expectation : int list =
                [ 4; 8; 16; 32; 64; 128; 256; 512; 1024; 2048; 4096 ]
            Assert.AreEqual(expectation, results)

            let verification =
                readFrom Position.Start
                |> Seq.take 11
                |> Seq.map (fst >> Event.data)
                |> Seq.toList
            Assert.AreEqual(expectation, verification)
        | _ ->
            Assert.Ignore("Competitor, ReaderFrom or Writer is reported to be unsupported by this source.")
           
    abstract member TestReadFromBeforeEnd : unit->unit
    
    [<Test>]
    default x.TestReadFromBeforeEnd() =
        let connection = x.Connect()
        let write = writer connection
        let readFrom = readerFrom connection
        let subscribeFrom = subscriberFrom connection
        let event_0 = event("TestReadFromBeforeEnd-0")
        let event_1 = event("TestReadFromBeforeEnd-1")
        let event_2 = event("TestReadFromBeforeEnd-2")
        write event_0
        write event_1
        write event_2
        let flush =
            subscribeFrom Position.Start
            |> Seq.take 3
        let take position =
            readFrom position
            |> Seq.map (fst >> Event.data)
            |> Seq.head
        Assert.AreEqual([], List.ofSeq(readFrom(Position.End)))
        Assert.AreEqual(event_2.Data,take(Position.Before(Position.End)))
        Assert.AreEqual(event_1.Data,take(Position.Before(Position.Before(Position.End))))
        Assert.AreEqual(event_0.Data,take(Position.Before(Position.Before(Position.Before(Position.End)))))
        
    abstract member TestReadFromBeforeEndAsSnapshot : unit->unit

    [<Test>]
    default x.TestReadFromBeforeEndAsSnapshot() =
        let connection = x.Connect()
        let write, readFrom =
            writer connection,
            readerFrom connection
        let flush =
            let subscribeFrom = subscriberFrom connection
            fun n ->
                subscribeFrom(Position.Start)
                |> Seq.take n
                |> Seq.iter ignore
        let snapshot _ =
            readFrom (Position.Before Position.End)
            |> Seq.tryPick (fst >> Event.data >> Some)
            |> Option.toList
        Assert.AreEqual([], snapshot())
        let events =
            [ event("TestReadFromBeforeEnd-0")
              event("TestReadFromBeforeEnd-1")
              event("TestReadFromBeforeEnd-2") ]
        write events.[0]
        flush 1
        Assert.AreEqual([events.[0].Data], snapshot())
        write events.[1]
        flush 2
        Assert.AreEqual([events.[1].Data], snapshot())
        write events.[2]
        flush 3
        Assert.AreEqual([events.[2].Data], snapshot())

    abstract member TestSubscribeFromBeforeEnd : unit->unit

    [<Test>]
    default x.TestSubscribeFromBeforeEnd() =
        let connection = x.Connect()
        let write = writer connection
        let readFrom = readerFrom connection
        let subscribeFrom = subscriberFrom connection

        let pretake position =
            let subscription = subscribeFrom position
            lazy(
                subscription
                |> Seq.map (fst >> Event.data)
                |> Seq.head)
        let pretakeBeforeEnd =
            pretake(Position.Before(Position.End))
        let pretakeBeforeBeforeEnd =
            pretake(Position.Before(Position.Before(Position.End)))
        let pretakeBeforeBeforeBeforeEnd =
            pretake(Position.Before(Position.Before(Position.Before(Position.End))))

        let event_0 = event("TestSubscribeFromBeforeEnd-0")
        let event_1 = event("TestSubscribeFromBeforeEnd-1")
        let event_2 = event("TestSubscribeFromBeforeEnd-2")
        write event_0
        write event_1
        write event_2
        let flush =
            subscribeFrom Position.Start
            |> Seq.take 3
        let take position =
            subscribeFrom position
            |> Seq.map (fst >> Event.data)
            |> Seq.head
        Assert.AreEqual(event_2.Data,take(Position.Before(Position.End)))
        Assert.AreEqual(event_1.Data,take(Position.Before(Position.Before(Position.End))))
        Assert.AreEqual(event_0.Data,take(Position.Before(Position.Before(Position.Before(Position.End)))))
        // the following subscribed when there was no data on the stream:
        // waiting for the value before the end position should always return the first
        Assert.AreEqual(event_0.Data,pretakeBeforeEnd.Force())
        Assert.AreEqual(event_0.Data,pretakeBeforeBeforeEnd.Force())
        Assert.AreEqual(event_0.Data,pretakeBeforeBeforeBeforeEnd.Force())
