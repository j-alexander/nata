namespace Nata.IO.CosmosDB.Tests

open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

[<TestFixture>]
type QueryTests() =

    let connect(collection) =
        let collection =
            { Collection.Endpoint = Endpoint.emulator
              Collection.Database = guid()
              Collection.Name = guid() }
        Document.connect collection
        |> Source.mapData (JsonValue.Codec.createBytesToType())
        ,
        Query.connect collection
        |> Source.mapData (JsonValue.Codec.createBytesToType())

    [<Test>]
    member x.TestQueryAll() =
        let documentFor, queryFor = connect()
        let documentWithId =
            [ for i in 1..10 -> { TestDocument.Value=guid() }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let expect =
            documentWithId
            |> List.map fst
            |> List.sortBy (fun { Value=x } -> x)

        let result =
            Channel.reader (queryFor "select * from c") ()
            |> Seq.map (Event.data)
            |> Seq.sortBy (fun { Value=x } -> x)
            |> Seq.toList

        Assert.AreEqual(expect, result)

    [<Test>]
    member x.TestQueryAllFromStart() =
        let documentFor, queryFor = connect()
        let documentWithId =
            [ for i in 1..10 -> { TestDocument.Value=guid() }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let expect =
            documentWithId
            |> List.map fst
            |> List.sortBy (fun { Value=x } -> x)

        let result =
            Position.Start
            |> Channel.readerFrom (queryFor "select * from c")
            |> Seq.map (fst >> Event.data)
            |> Seq.sortBy (fun { Value=x } -> x)
            |> Seq.toList

        Assert.AreEqual(expect, result)