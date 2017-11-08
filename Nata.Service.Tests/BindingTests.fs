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
        let result =
            let readFrom = Channel.readerFrom output
            readFrom(Position.Before Position.End)
            |> Seq.map (fst >> Event.data >> Consumer.state)
            |> Seq.head
        Assert.AreEqual(list, result)
        Assert.AreEqual(
            [["c"];["b";"c"];["a";"b";"c"]],
            states
            |> Seq.map Consumer.state
            |> Seq.toList)
