namespace Nata.IO.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type ChannelTests() as x =

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

    abstract member Connect : unit -> Source<string,byte[],int64>
    abstract member Channel : unit -> string

    member private x.Capabilities() = x.Channel() |> x.Connect()

    [<Test>]
    member x.TestConnect() =
        let stream = x.Capabilities()
        Assert.Greater(stream.Length, 0)

    [<Test>]
    member x.TestWrite() =
        let write = x.Capabilities() |> writer
        let event = event("TestWrite")
        write event

    [<Test>]
    member x.TestRead() =
        let connection = x.Capabilities()
        let write = writer connection
        let read = reader connection
        let event = event("TestRead")
        write event

        let result = read() |> Seq.head
        Assert.AreEqual(event.Data, result.Data)

    [<Test>]
    member x.TestReadFrom() =
        let connection = x.Capabilities()
        let write = writer connection
        let readFrom = readerFrom connection
        let event_0 = event("TestReadFrom-0")
        let event_1 = event("TestReadFrom-1")
        write event_0
        write event_1
        
        let result, index = readFrom (Position.At 0L) |> Seq.head
        Assert.AreEqual(event_0.Data, result.Data)
        Assert.AreEqual(0, index)
        
        let result, index = readFrom (Position.At 1L) |> Seq.head
        Assert.AreEqual(event_1.Data, result.Data)
        Assert.AreEqual(1, index)
        
        let result, index = readFrom (Position.Start) |> Seq.head
        Assert.AreEqual(event_0.Data, result.Data)
        Assert.AreEqual(0, index)

    [<Test>]
    member x.TestWriteTo() =
        let connection = x.Capabilities()
        match tryWriterTo connection with
        | Some writeTo ->
            [ 0L, event "TestWriteTo-0" |> writeTo (Position.At 0L)
              1L, event "TestWriteTo-1" |> writeTo (Position.At 1L)
              2L, event "TestWriteTo-2" |> writeTo (Position.At 2L) ]
            |> List.iter Assert.AreEqual
        | _ ->
            Assert.Pass("WriterTo is reported to be unsupported by this source.")

    [<Test>]
    member x.TestWriteToPosition() =
        let connection = x.Capabilities()
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
            Assert.Pass("ReaderFrom and WriterTo are reported to be unsupported by this source.")

    [<Test; ExpectedException(typeof<Position.Invalid<int64>>)>]
    member x.TestWriteToShouldFailWithIndexTooLow() =
        let connection = x.Capabilities()
        match tryWriterTo connection with
        | Some writeTo ->
            event "TestWriteToShouldFailWithIndexTooLow-0" |> writeTo (Position.At -1L) |> ignore
            event "TestWriteToShouldFailWithIndexTooLow-1" |> writeTo (Position.At 0L) |> ignore
            event "TestWriteToShouldFailWithIndexTooLow-2" |> writeTo (Position.At 0L) |> ignore
        | _ ->
            Assert.Pass("WriterTo is reported to be unsupported by this source.")

    [<Test; ExpectedException(typeof<Position.Invalid<int64>>)>]
    member x.TestWriteToShouldFailWithIndexTooHigh() =
        let connection = x.Capabilities()
        match tryWriterTo connection with
        | Some writeTo ->
            event "TestWriteToShouldFailWithIndexTooHigh-0" |> writeTo (Position.At -1L) |> ignore
            event "TestWriteToShouldFailWithIndexTooHigh-1" |> writeTo (Position.At 0L) |> ignore
            event "TestWriteToShouldFailWithIndexTooHigh-2" |> writeTo (Position.At 2L) |> ignore
        | _ ->
            Assert.Pass("WriterTo is reported to be unsupported by this source.")
        
    [<Test; Timeout(120000)>]
    member x.TestLiveSubscription() =
        let write, subscribe =
            let connection = x.Capabilities()
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
            let connection= x.Capabilities()
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
            let connection = x.Capabilities()
            writer connection, subscriberFrom connection
        let expected =
            [| event "TestLateSubscriptionFromIndex-0"
               event "TestLateSubscriptionFromIndex-1"
               event "TestLateSubscriptionFromIndex-2" |]
        for event in expected do
            write event
        subscribeFrom (Position.Start)
        |> Seq.take 3
        |> Seq.toArray
        |> Array.zip expected
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At 0L)
        |> Seq.take 3
        |> Seq.toArray
        |> Array.zip expected
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At 1L)
        |> Seq.take 2
        |> Seq.toArray
        |> Array.zip (expected.[1..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))
        subscribeFrom (Position.At 2L)
        |> Seq.take 1
        |> Seq.toArray
        |> Array.zip (expected.[2..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Data, actual.Data))