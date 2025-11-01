namespace Nata.IO.CosmosDB.Tests

open System
open System.Threading
open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

type TestDocument = {
    Text : string
}
type TestNumber = {
    Value : int
}

[<TestFixture>]
type DocumentTests() = 

    let connect() =
        Document.connect { Container.Settings.Endpoint = Endpoint.emulator
                           Container.Settings.Database = guid()
                           Container.Settings.Name = guid() }

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

        let expect = { Text = guid() }

        expect
        |> Event.create
        |> write

        let result =
            read()
            |> Seq.map Event.data
            |> Seq.head

        Assert.AreEqual(expect.Text, result.Text)

    [<Test>]
    member x.TestOptimisticConcurrency() =
        let channel = channel()
        let readFrom = Channel.readerFrom channel
        let writeTo (position) =
            { Text = guid() }
            |> Event.create
            |> Channel.writerTo channel position

        Assert.Throws<Position.Invalid<string>>(fun _ ->
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

        Assert.Throws<Position.Invalid<string>>(fun _ ->
            writeTo (Position.At created)
            |> ignore)
        |> ignore

        let overwritten =
            writeTo (Position.End)

        Assert.Throws<Position.Invalid<string>>(fun _ ->
            writeTo (Position.At updated)
            |> ignore)
        |> ignore

        writeTo (Position.At overwritten)
        |> ignore

    [<Test>]
    member x.TestTwoCompetitors() =
            
        let verify (firstMsDelays, secondMsDelays) =
            let compete, readFrom =
                let channel = channel()
                Channel.competitor channel,
                Channel.readerFrom channel
  
            let generation (delay:int->int) : int seq =
                compete (Option.defaultValue (Event.create { Value=2 }) >> fun e ->
                    let input = Event.data e
                    let output = { Value = input.Value * 2 }
                    Thread.Sleep(delay input.Value)
                    Event.create output)
                |> Seq.take 11
                |> Seq.map (Event.data >> fun { Value=x } -> x)

        let results : int list =
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

        //let results =
        //  [ 4; 8; 16; 32; 64; 128; 256;   4;  512;    8;   16 ]
        let expectation : int list =
            [ 4; 8; 16; 32; 64; 128; 256; 512; 1024; 2048; 4096 ]
        Assert.AreEqual(expectation, results) // TODO: FAIL
        Assert.LessOrEqual(
            4096,
            readFrom Position.End
            |> Seq.map (fst >> Event.data >> fun { Value=x } -> x)
            |> Seq.head)