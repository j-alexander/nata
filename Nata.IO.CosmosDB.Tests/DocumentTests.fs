namespace Nata.IO.CosmosDB.Tests

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