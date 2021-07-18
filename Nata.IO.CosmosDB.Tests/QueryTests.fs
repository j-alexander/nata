namespace Nata.IO.CosmosDB.Tests

open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

[<TestFixture>]
type QueryTests() =

    let connect(collection) =
        let collection : Container.Settings =
            { Endpoint = Endpoint.emulator
              Database = guid()
              Name = guid() }
        Document.connect collection
        |> Source.mapData (JsonValue.Codec.createBytesToType())
        ,
        Query.connect collection
        |> Source.mapData (JsonValue.Codec.createBytesToType())
        ,
        Query.connectWithParameters collection
        |> Source.mapData (JsonValue.Codec.createBytesToType())

    [<Test>]
    member x.TestQueryAll() =
        let documentFor, queryFor, _ = connect()
        let documentWithId =
            [ for i in 1..10 -> { Text=guid() }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let expect =
            documentWithId
            |> List.map fst
            |> List.sortBy (fun { Text=x } -> x)

        let result =
            Channel.reader (queryFor "select * from c") ()
            |> Seq.map (Event.data)
            |> Seq.sortBy (fun { Text=x } -> x)
            |> Seq.toList

        Assert.AreEqual(expect, result)

    [<Test>]
    member x.TestQueryWithParameterRange() =
        let documentFor, _, queryFor = connect()
        let documentWithId =
            [ for i in 1..10 -> { Value=i }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let expect =
            documentWithId
            |> List.map fst
            |> List.filter (fun { Value=x } -> x > 7)
            |> List.sortBy (fun { Value=x } -> x)

        let result =
            let queryWithParameters  : Query*Parameters =
                """select * from c where c["Value"] > @v""",
                Map [ "@v", 7 :> obj ]
            Channel.reader (queryFor queryWithParameters) ()
            |> Seq.map (Event.data)
            |> Seq.sortBy (fun { Value=x } -> x)
            |> Seq.toList

        Assert.AreEqual(expect, result)

    [<Test>]
    member x.TestQueryAllFromStart() =
        let documentFor, queryFor, _ = connect()
        let documentWithId =
            [ for i in 1..10 -> { Text=guid() }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let expect =
            documentWithId
            |> List.map fst
            |> List.sortBy (fun { Text=x } -> x)

        let result =
            Position.Start
            |> Channel.readerFrom (queryFor "select * from c")
            |> Seq.map (fst >> Event.data)
            |> Seq.sortBy (fun { Text=x } -> x)
            |> Seq.toList

        Assert.AreEqual(expect, result)

    [<Test>]
    member x.TestQueryEachFromPositionInAll() =
        let documentFor, queryFor, _ = connect()
        let documentWithId =
            [ for i in 1..10 -> { Text=guid() }, sprintf "%d" i ]

        for document, id in documentWithId do
            document
            |> Event.create
            |> Channel.writer (documentFor id)

        let query = queryFor "select * from c"
        let expected =
            Channel.readerFrom query Position.Start
            |> Seq.map (fun ({ Event.Data={ Text=text } }, token) -> text, token)
            |> Seq.toList

        Assert.AreEqual(10, expected.Length)

        for expected, token in expected do
            let result, _ =
                Position.At token
                |> Channel.readerFrom query
                |> Seq.mapFst (Event.data >> fun { Text=x } -> x)
                |> Seq.head
            Assert.AreEqual(expected, result)

        Assert.AreEqual(
            fst expected.[8],
            Position.After(Position.After(Position.After(Position.After(Position.At(snd expected.[4])))))
            |> Channel.readerFrom query
            |> Seq.map (fst >> Event.data >> fun { Text=x } -> x)
            |> Seq.head)
        Assert.AreEqual(
            fst expected.[4],
            Position.After(Position.After(Position.After(Position.After(Position.Start))))
            |> Channel.readerFrom query
            |> Seq.map (fst >> Event.data >> fun { Text=x } -> x)
            |> Seq.head)
        Assert.True(
            Position.End
            |> Channel.readerFrom query
            |> Seq.isEmpty)
        Assert.True(
            Position.After(Position.End)
            |> Channel.readerFrom query
            |> Seq.isEmpty)
        Assert.True(
            Position.After(Position.After(Position.End))
            |> Channel.readerFrom query
            |> Seq.isEmpty)