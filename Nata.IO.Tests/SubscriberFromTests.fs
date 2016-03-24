namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type SubscriberFromTests() =

    let event =
        Event.create ()
        |> Event.Source.withName "event_name"
        |> Event.Source.withEventType "event_type"
        |> Event.Source.withStream "event_stream"

    abstract member Connect : unit -> List<Capability<'Data,int64>>
    
    member x.Connect(fn) =
        let stream = x.Connect()
        stream |> subscriberFrom |> fn,
        stream |> writer

    [<Test>]
    member x.MapValueTest() =
        let subscribeFrom, write = x.Connect(SubscriberFrom.mapData ((+) 1))

        let input = [1;2;3]
        let output = [2;3;4]

        let run() =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            subscribeFrom 0L |> Seq.take 3 |> Seq.map (fst >> Event.data) |> Seq.toList

        Assert.AreEqual(output, run())

    [<Test>]
    member x.MapTypeTest() =
        let subscribeFrom, write = x.Connect(SubscriberFrom.mapData (fun x -> x.ToString()))

        let input = [1;2;3]
        let output = ["1";"2";"3"]

        let run() =
            for i in input do
                event |> Event.mapData (fun _ -> i) |> write
            subscribeFrom 0L |> Seq.take 3 |> Seq.map (fst >> Event.data) |> Seq.toList

        Assert.AreEqual(output, run())