namespace Nata.IO.File.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability
open Nata.IO.File
open Nata.IO.File.Stream

[<TestFixture>]
type StreamTests() =

    let date = DateTime.UtcNow
    let event fn i =
        { Data = JsonValue.Number i
          Metadata = JsonValue.String (Assembly.GetExecutingAssembly().FullName)
          Date = date
          Stream = "Nata.IO.File.Tests"
          Type = fn }

    let connect() = 
        Stream.create(Path.GetTempFileName())

    [<Test>]
    member x.TestConcurrency() =
        let stream = connect()
        let read,write =
            Nata.IO.Capability.reader stream,
            Nata.IO.Capability.writer stream
             
        let format = event "StreamTests.TestConcurrency"
           
        let work =
            seq {
                yield async {
                    [1m..10000m] |> Seq.iter (format >> write)
                    return 0
                }
                for reader in 1..40 -> async {
                    do! Async.Sleep(10*reader)
                    return
                        read()
                        |> Seq.mapi(fun i actual -> 
                            let expected = i |> decimal |> (+) 1m |> format
                            Assert.AreEqual(expected, actual))
                        |> Seq.length
                }
            }

        let results =
            Async.Parallel work
            |> Async.RunSynchronously
            |> Seq.sum

        Assert.Greater(results, 0, "should read some valid data")


    [<Test>]
    member x.TestConnect() =
        let stream = connect()
        Assert.Greater(stream.Length, 0)

    [<Test>]
    member x.TestWrite() =
        let write = connect() |> writer
        let event = event "StreamTests.TestWrite" 0m
        write event

    [<Test>]
    member x.TestRead() =
        let connection = connect()
        let write = writer connection
        let read = reader connection
        let event = event "StreamTests.TestRead" 0m
        write event

        let result = read() |> Seq.head
        Assert.AreEqual(event.Data, result.Data)
        Assert.AreEqual(event.Metadata, result.Metadata)

    [<Test>]
    member x.TestReadFrom() =
        let connection = connect()
        let write = writer connection
        let readFrom = readerFrom connection
        let event_0 = event "StreamTests.TestReadFrom" 0m
        let event_1 = event "StreamTests.TestReadFrom" 1m
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
        event "StreamTests.TestWriteTo" 0m |> writeTo Stream.Empty |> ignore
        event "StreamTests.TestWriteTo" 1m |> writeTo 0 |> ignore
        event "StreamTests.TestWriteTo" 2m |> writeTo 1 |> ignore

    [<Test; ExpectedException(typeof<InvalidPosition<string,Index>>)>]
    member x.TestWriteToShouldFailWithIndexTooLow() =
        let writeTo, readFrom =
            let connection = connect()
            writerTo connection, readerFrom connection
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow" 0m |> writeTo Stream.Empty |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow" 1m |> writeTo 0 |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooLow" 2m |> writeTo 0 |> ignore

    [<Test; ExpectedException(typeof<InvalidPosition<string,Index>>)>]
    member x.TestWriteToShouldFailWithIndexTooHigh() =
        let writeTo, readFrom =
            let connection = connect()
            writerTo connection, readerFrom connection
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh" 0m |> writeTo Stream.Empty |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh" 1m |> writeTo 0 |> ignore
        event "StreamTests.TestWriteToShouldFailWithIndexTooHigh" 2m |> writeTo 2 |> ignore
        
//    [<Test; Timeout(15000)>]
//    member x.TestLiveSubscription() =
//        let write, subscribe =
//            let connection= connect()
//            writer connection, subscriber connection
//        let results = subscribe()
//        let expected =
//            [ event "StreamTests.TestLiveSubscription-0"
//              event "StreamTests.TestLiveSubscription-1"
//              event "StreamTests.TestLiveSubscription-2" ]
//        for event in expected do
//            write event
//        results
//        |> Seq.take 3
//        |> Seq.toList
//        |> List.zip expected
//        |> List.iter(fun (expected, actual) ->
//            Assert.AreEqual(expected.Type, actual.Type)
//            Assert.AreEqual(expected.Data, actual.Data)
//            Assert.AreEqual(expected.Metadata, actual.Metadata))
//            
//    [<Test; Timeout(15000)>]
//    member x.TestLateSubscription() =
//        let write, subscribe =
//            let connection= connect()
//            writer connection, subscriber connection
//        let expected =
//            [ event "StreamTests.TestLateSubscription-0"
//              event "StreamTests.TestLateSubscription-1"
//              event "StreamTests.TestLateSubscription-2" ]
//        for event in expected do
//            write event
//        subscribe()
//        |> Seq.take 3
//        |> Seq.toList
//        |> List.zip expected
//        |> List.iter(fun (expected, actual) ->
//            Assert.AreEqual(expected.Type, actual.Type)
//            Assert.AreEqual(expected.Data, actual.Data)
//            Assert.AreEqual(expected.Metadata, actual.Metadata))
//            
//    [<Test; Timeout(15000)>]
//    member x.TestSubscriptionFromIndex() =
//        let write, subscribeFrom =
//            let connection= connect()
//            writer connection, subscriberFrom connection
//        let expected =
//            [| event "StreamTests.TestLateSubscription-0"
//               event "StreamTests.TestLateSubscription-1"
//               event "StreamTests.TestLateSubscription-2" |]
//        for event in expected do
//            write event
//        subscribeFrom 0
//        |> Seq.take 3
//        |> Seq.toArray
//        |> Array.zip expected
//        |> Array.iter(fun (expected, (actual, index)) ->
//            Assert.AreEqual(expected.Type, actual.Type)
//            Assert.AreEqual(expected.Data, actual.Data)
//            Assert.AreEqual(expected.Metadata, actual.Metadata))
//        subscribeFrom 1
//        |> Seq.take 2
//        |> Seq.toArray
//        |> Array.zip (expected.[1..2])
//        |> Array.iter(fun (expected, (actual, index)) ->
//            Assert.AreEqual(expected.Type, actual.Type)
//            Assert.AreEqual(expected.Data, actual.Data)
//            Assert.AreEqual(expected.Metadata, actual.Metadata))
//        subscribeFrom 2
//        |> Seq.take 1
//        |> Seq.toArray
//        |> Array.zip (expected.[2..2])
//        |> Array.iter(fun (expected, (actual, index)) ->
//            Assert.AreEqual(expected.Type, actual.Type)
//            Assert.AreEqual(expected.Data, actual.Data)
//            Assert.AreEqual(expected.Metadata, actual.Metadata))