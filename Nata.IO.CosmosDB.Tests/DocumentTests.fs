namespace Nata.IO.CosmosDB.Tests

open System
open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

type TestDocument = {
    Value : string
}

[<TestFixture>]
type DocumentTests() = 

    let connect() =
        Document.connect { Collection.Endpoint = Endpoint.emulator
                           Collection.Database = guid()
                           Collection.Name = guid() }

    let channel() =
        connect()
        |> Source.mapData (JsonValue.Codec.createBytesToType())
        <| guid()

    [<Test>]
    member x.TestReadWrite() =
        let read, write =
            let channel = channel()
            Channel.reader channel,
            Channel.writer channel

        let expect = { TestDocument.Value = guid() }

        expect
        |> Event.create
        |> write

        let result =
            read()
            |> Seq.map Event.data
            |> Seq.head

        Assert.AreEqual(expect.Value, result.Value)

    [<Test>]
    member x.TestOptimisticConcurrency() =
        let channel = channel()
        let readFrom = Channel.readerFrom channel
        let writeTo (position) =
            { TestDocument.Value = guid() }
            |> Event.create
            |> Channel.writerTo channel position

        Assert.Throws<AggregateException>(fun _ ->
            writeTo (Position.At "\"00001700-0000-0000-0000-5a15e0b90000\"")
            |> ignore)
        |> ignore

        let created =
            writeTo (Position.Start)

        let _, createdResult =
            readFrom Position.End
            |> Seq.head
        Assert.AreEqual(created, createdResult)

        let updated =
            writeTo (Position.At created)

        Assert.Throws<AggregateException>(fun _ ->
            writeTo (Position.At created)
            |> ignore)
        |> ignore

        let overwritten =
            writeTo (Position.End)

        Assert.Throws<AggregateException>(fun _ ->
            writeTo (Position.At updated)
            |> ignore)
        |> ignore

        writeTo (Position.At overwritten)
        |> ignore
