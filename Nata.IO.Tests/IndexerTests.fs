namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Channel

[<AbstractClass>]
type IndexerTests() =

    let event =
        Event.create "data"
        |> Event.withName "event_name"
        |> Event.withEventType "event_type"
        |> Event.withStream "event_stream"

    abstract member Connect : unit -> Channel<'Data,int64>

    member x.Connect(fn) =
        let stream = x.Connect()
        stream |> writer,
        stream |> indexer |> fn

    [<Test>]
    member x.TestPositionsWhenEmpty() =
        let _, index = x.Connect(id);
        let start = index Position.Start
        let finish = index Position.End
        Assert.AreEqual(start, finish)

    [<Test>]
    member x.TestInductivePosition() =
        let write, index = x.Connect(id)

        let before = index Position.Start
        write event
        let after = index Position.End

        Assert.AreEqual(1L + before, after)

        let previous = index (Position.Before Position.End)
        Assert.AreEqual(before, previous)

        let next = index (Position.After Position.Start)
        Assert.AreEqual(after, next)
        
