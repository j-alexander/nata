namespace Nata.IO.EventStore.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework
open EventStore.ClientAPI

open Nata.IO
open Nata.IO.Capability
open Nata.IO.EventStore

[<TestFixture>]
type StreamTests() = 

    let settings : Settings =
        { Server = { Host = "localhost"
                     Port = 1113 }
          User = { Name = "admin"
                   Password = "changeit" } }

    let connect() =
        let stream = Guid.NewGuid().ToString("n")
        Stream.connect settings stream

    let event(fn) =
        { Data =
            [| "case", JsonValue.String fn
               "at", JsonValue.String (DateTime.Now.ToString()) 
            |] |> JsonValue.Record
               |> JsonValue.toBytes
          Metadata =
            [| "from", JsonValue.String (Assembly.GetExecutingAssembly().FullName)
            |] |> JsonValue.Record
               |> JsonValue.toBytes
          Date = DateTime.UtcNow
          Stream = null
          Type = fn }

    [<Test>]
    member x.TestConnect() =
        let stream = connect()
        Assert.Greater(stream.Length, 0)

    [<Test>]
    member x.TestWrite() =
        let write = connect() |> writer
        let event = event("StreamTests.TestWrite")
        write event

    [<Test>]
    member x.TestRead() =
        let connection = connect()
        let write = writer connection
        let read = reader connection
        let event = event("StreamTests.TestRead")
        write event

        let result = read() |> Seq.head
        Assert.AreEqual(event.Data, result.Data)
        Assert.AreEqual(event.Metadata, result.Metadata)

    [<Test>]
    member x.TestReadFrom() =
        let connection = connect()
        let write = writer connection
        let readFrom = readerFrom connection
        let event_0 = event("StreamTests.TestReadFrom-0")
        let event_1 = event("StreamTests.TestReadFrom-1")
        write event_0
        write event_1
        
        let result, index = readFrom 0 |> Seq.head
        Assert.AreEqual(event_0.Data, result.Data)
        Assert.AreEqual(event_0.Metadata, result.Metadata)
        Assert.AreEqual(0, index)
        
        let result, index = readFrom 1 |> Seq.head
        Assert.AreEqual(event_1.Data, result.Data)
        Assert.AreEqual(event_1.Metadata, result.Metadata)
        Assert.AreEqual(1, index)

    [<Test>]
    member x.TestWriteTo() =
        let writeTo, readFrom =
            let connection = connect()
            writerTo connection, readerFrom connection
        event "StreamTests.TestWriteTo-0" |> writeTo ExpectedVersion.NoStream |> ignore
        event "StreamTests.TestWriteTo-1" |> writeTo 0 |> ignore
        event "StreamTests.TestWriteTo-2" |> writeTo 1 |> ignore

    [<Test; ExpectedException(typeof<AggregateException>)>]
    member x.TestWriteToShouldFailWithIndexTooLow() =
        let writeTo, readFrom =
            let connection = connect()
            writerTo connection, readerFrom connection
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow-0" |> writeTo ExpectedVersion.NoStream |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow-1" |> writeTo 0 |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow-2" |> writeTo 0 |> ignore

    [<Test; ExpectedException(typeof<AggregateException>)>]
    member x.TestWriteToShouldFailWithIndexTooHigh() =
        let writeTo, readFrom =
            let connection = connect()
            writerTo connection, readerFrom connection
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh-0" |> writeTo ExpectedVersion.NoStream |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh-1" |> writeTo 0 |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh-2" |> writeTo 2 |> ignore
        
    [<Test; Timeout(15000)>]
    member x.TestLiveSubscription() =
        let write, subscribe =
            let connection= connect()
            writer connection, subscriber connection
        let results = subscribe()
        let expected =
            [ event "StreamTests.TestLiveSubscription-0"
              event "StreamTests.TestLiveSubscription-1"
              event "StreamTests.TestLiveSubscription-2" ]
        for event in expected do
            write event
        results
        |> Seq.take 3
        |> Seq.toList
        |> List.zip expected
        |> List.iter(fun (expected, actual) ->
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
            
    [<Test; Timeout(15000)>]
    member x.TestLateSubscription() =
        let write, subscribe =
            let connection= connect()
            writer connection, subscriber connection
        let expected =
            [ event "StreamTests.TestLateSubscription-0"
              event "StreamTests.TestLateSubscription-1"
              event "StreamTests.TestLateSubscription-2" ]
        for event in expected do
            write event
        subscribe()
        |> Seq.take 3
        |> Seq.toList
        |> List.zip expected
        |> List.iter(fun (expected, actual) ->
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
            
    [<Test; Timeout(15000)>]
    member x.TestSubscriptionFromIndex() =
        let write, subscribeFrom =
            let connection= connect()
            writer connection, subscriberFrom connection
        let expected =
            [| event "StreamTests.TestLateSubscription-0"
               event "StreamTests.TestLateSubscription-1"
               event "StreamTests.TestLateSubscription-2" |]
        for event in expected do
            write event
        subscribeFrom 0
        |> Seq.take 3
        |> Seq.toArray
        |> Array.zip expected
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
        subscribeFrom 1
        |> Seq.take 2
        |> Seq.toArray
        |> Array.zip (expected.[1..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
        subscribeFrom 2
        |> Seq.take 1
        |> Seq.toArray
        |> Array.zip (expected.[2..2])
        |> Array.iter(fun (expected, (actual, index)) ->
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))

    