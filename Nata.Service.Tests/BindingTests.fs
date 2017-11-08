namespace Nata.Service.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.Capability
open Nata.IO.Memory
open Nata.Service

[<TestFixture>]
type BindingTests() =

    let channel() =
        Stream.connect()
        <| guid()

    let snapshot channel =
        let readFrom = Channel.readerFrom channel
        readFrom(Position.Before Position.End)
        |> Seq.map (fst >> Event.data >> Consumer.state)
        |> Seq.head

    [<Test>]
    member x.TestFold() =
        let input, output =
            channel(), channel()
        let fn state x =
            let xs = Option.defaultValue [] state
            x :: xs
        let writeAll =
            let write = Event.create >> Channel.writer input
            List.rev >> List.iter write
        let list = ["a"; "b"; "c"]
        list |> writeAll
        let states =
            input
            |> Binding.fold fn output
            |> Seq.take 3
            |> Seq.toList
        let result = snapshot output
        Assert.AreEqual(list, result)
        Assert.AreEqual(
            [["c"];["b";"c"];["a";"b";"c"]],
            states
            |> Seq.map Consumer.state
            |> Seq.toList)

    [<Test>]
    member x.TestMap() =
        let input, output =
            channel(), channel()
        let fn x = x * 2
        [1..5]
        |> List.iter (Event.create >> Channel.writer input)
        let states =
            input
            |> Binding.map fn output
            |> Seq.take 5
            |> Seq.map Consumer.state
            |> Seq.toList
        Assert.AreEqual([2;4;6;8;10], states)
        Assert.AreEqual(10, snapshot output)
