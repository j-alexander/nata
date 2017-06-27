namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Channel

[<AbstractClass>]
type WriterTests() =

    let event =
        Event.create "0"
        |> Event.withName "event_name"
        |> Event.withEventType "event_type"
        |> Event.withStream "event_stream"

    abstract member Connect : unit -> Channel<'Data,int64>

    member x.Connect(fn) =
        let stream = x.Connect()
        stream |> reader,
        stream |> writer |> fn

    [<Test>]
    member x.MapValueTest() =
        let read, write = x.Connect(Writer.mapData ((+) 1))

        let input = [1;2;3]
        let output = [2;3;4]

        let run =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            read >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())

    [<Test>]
    member x.MapTypeTest() =
        let read, write = x.Connect(Writer.mapData (fun x -> x.ToString()))

        let input = [1;2;3]
        let output = ["1";"2";"3"]

        let run =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            read >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())