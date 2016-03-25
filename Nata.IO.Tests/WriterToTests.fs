namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type WriterToTests() =

    let event =
        Event.create ()
        |> Event.withName "event_name"
        |> Event.withEventType "event_type"
        |> Event.withStream "event_stream"

    abstract member Connect : unit -> List<Capability<'Data,int64>>
    
    member x.Connect(fn) =
        let stream = x.Connect()
        stream |> reader,
        stream |> writerTo |> fn

    [<Test>]
    member x.MapValueTest() =
        let read, writeTo = x.Connect(WriterTo.mapData ((+) 1))

        let input = [1;2;3]
        let output = [2;3;4]

        let run =
            for at, i in input |> Seq.mapi (fun at i -> at-1,i) do
                event |> Event.mapData (fun _ -> i) |> writeTo (int64 at) |> ignore
            read >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())

    [<Test>]
    member x.MapTypeTest() =
        let read, writeTo = x.Connect(WriterTo.mapData (fun x -> x.ToString()))

        let input = [1;2;3]
        let output = ["1";"2";"3"]

        let run =
            for at, i in input |> Seq.mapi (fun at i -> at-1,i) do
                event |> Event.mapData (fun _ -> i) |> writeTo (int64 at) |> ignore
            read >> Seq.map Event.data >> Seq.toList

        Assert.AreEqual(output, run())