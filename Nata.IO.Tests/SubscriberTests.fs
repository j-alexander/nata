namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type SubscriberTests() =

    let event =
        Event.create ()
        |> Event.withName "event_name"
        |> Event.withEventType "event_type"
        |> Event.withStream "event_stream"

    abstract member Connect : unit -> List<Capability<'Data,int64>>

    member x.Connect(fn) =
        let stream = x.Connect()
        stream |> subscriber |> fn,
        stream |> writer

    [<Test>]
    member x.MapValueTest() =
        let subscribe, write = x.Connect(Reader.mapData ((+) 1))

        let input = [1;2;3]
        let output = [2;3;4]

        let run =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            subscribe >> Seq.take 3 >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())

    [<Test>]
    member x.MapTypeTest() =
        let subscribe, write = x.Connect(Reader.mapData (fun x -> x.ToString()))

        let input = [1;2;3]
        let output = ["1";"2";"3"]

        let run =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            subscribe >> Seq.take 3  >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())