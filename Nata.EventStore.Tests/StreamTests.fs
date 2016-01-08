namespace Nata.EventStore.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework
open EventStore.ClientAPI

open Nata.IO
open Nata.EventStore

[<TestFixture>]
type StreamTests() = 

    let settings : Settings =
        { Server = { Host = "localhost"
                     Port = 1113 }
          User = { Name = "admin"
                   Password = "changeit" } }

    let connect() =
        let name = Guid.NewGuid().ToString("n")
        let connector = settings |> Stream.connect
        connector name

    let writer, writerTo, reader, readerFrom =
        List.pick (function Writer x -> Some x | _ -> None),
        List.pick (function WriterTo x -> Some x | _ -> None),
        List.pick (function Reader x -> Some x | _ -> None),
        List.pick (function ReaderFrom x -> Some x | _ -> None)

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
        