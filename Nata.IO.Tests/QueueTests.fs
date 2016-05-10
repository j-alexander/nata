namespace Nata.IO.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.JsonValue.Codec
open Nata.IO.Capability

[<AbstractClass>]
type QueueTests<'Channel>() =

    let event() =
        [| "text", JsonValue.String (Guid.NewGuid().ToString("n")) |]
        |> JsonValue.Record
        |> Event.create
        |> Event.map JsonValue.toBytes

    abstract member Connect : unit -> Source<'Channel,byte[],unit>
    abstract member Channel : unit -> 'Channel
    abstract member Stream : 'Channel -> string

    member private x.Capabilities() = x.Channel() |> x.Connect()

    [<Test; Timeout(15000)>]
    member x.TestWriteAndSubscribe() =
        let name = x.Channel()
        let queue, event = name |> x.Connect(), event()

        do writer queue event
        let result =
            subscribe queue
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        Assert.AreEqual(name |> x.Stream |> Some, result |> Event.stream)
        Assert.True(result |> Event.createdAt |> Option.isSome)

    [<Test; Timeout(15000)>]
    member x.TestWriteAndSubscribeMany() =
        let name = x.Channel()
        let queue = name |> x.Connect()

        let events =
            [ for i in [ 1..10 ] -> event() ]

        for event in events do
            event |> writer queue
        let results =
            subscribe queue
            |> Seq.take events.Length
            |> Seq.zip events

        for (before, after) in results do
            Assert.AreEqual(before.Data, after.Data)
            Assert.AreEqual(name |> x.Stream |> Some, after |> Event.stream)
            Assert.True(after |> Event.createdAt |> Option.isSome)