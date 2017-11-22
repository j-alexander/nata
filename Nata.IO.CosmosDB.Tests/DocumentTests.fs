namespace Nata.IO.CosmosDB.Tests

open System
open System.Threading
open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

type TestDocument = {
    Value : string
}
type TestNumber = {
    Value : int
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

    [<Test>]
    member x.TestTwoCompetitors() =
        let compete, readFrom, write =
            let channel = channel()
            Channel.competitor channel,
            Channel.readerFrom channel,
            Channel.writer channel

        let generation (delay:int->int) : int seq =
            compete (Option.defaultValue (Event.create { TestNumber.Value=2 }) >> fun e ->
                let input = Event.data e
                let output = { TestNumber.Value = input.Value * 2 }
                Thread.Sleep(delay input.Value)
                Event.create output)
            |> Seq.take 11
            |> Seq.map (Event.data >> fun { TestNumber.Value=x } -> x)

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
        Assert.AreEqual(
            8192,
            readFrom Position.End
            |> Seq.map (fst >> Event.data >> fun { TestNumber.Value=x } -> x)
            |> Seq.head)