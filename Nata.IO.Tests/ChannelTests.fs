namespace Nata.IO.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability

type Index = int

[<AbstractClass>]
type ChannelTests() as x =

    let event(fn) =
        { Data =
            [| "case", JsonValue.String (x.GetType().Name + "." + fn)
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

    abstract member Connect : unit -> Source<string,byte[],byte[],Index>
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
        Assert.AreEqual(event.Metadata, result.Metadata)

    [<Test>]
    member x.TestReadFrom() =
        let connection = x.Capabilities()
        let write = writer connection
        let readFrom = readerFrom connection
        let event_0 = event("TestReadFrom-0")
        let event_1 = event("TestReadFrom-1")
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
            let connection = x.Capabilities()
            writerTo connection, readerFrom connection
        event "TestWriteTo-0" |> writeTo -1 |> ignore
        event "TestWriteTo-1" |> writeTo 0 |> ignore
        event "TestWriteTo-2" |> writeTo 1 |> ignore

    [<Test; ExpectedException(typeof<InvalidPosition<string,Index>>)>]
    member x.TestWriteToShouldFailWithIndexTooLow() =
        let writeTo, readFrom =
            let connection = x.Capabilities()
            writerTo connection, readerFrom connection
        event "TestWriteToShouldFailWithIndexTooLow-0" |> writeTo -1 |> ignore
        event "TestWriteToShouldFailWithIndexTooLow-1" |> writeTo 0 |> ignore
        event "TestWriteToShouldFailWithIndexTooLow-2" |> writeTo 0 |> ignore

    [<Test; ExpectedException(typeof<InvalidPosition<string,Index>>)>]
    member x.TestWriteToShouldFailWithIndexTooHigh() =
        let writeTo, readFrom =
            let connection = x.Capabilities()
            writerTo connection, readerFrom connection
        event "TestWriteToShouldFailWithIndexTooHigh-0" |> writeTo -1 |> ignore
        event "TestWriteToShouldFailWithIndexTooHigh-1" |> writeTo 0 |> ignore
        event "TestWriteToShouldFailWithIndexTooHigh-2" |> writeTo 2 |> ignore
        
    [<Test; Timeout(15000)>]
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
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
            
    [<Test; Timeout(15000)>]
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
            Assert.AreEqual(expected.Type, actual.Type)
            Assert.AreEqual(expected.Data, actual.Data)
            Assert.AreEqual(expected.Metadata, actual.Metadata))
            
    [<Test; Timeout(15000)>]
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