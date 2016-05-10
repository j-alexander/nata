namespace Nata.IO.RabbitMQ.Tests

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
open Nata.IO.RabbitMQ

[<TestFixture>]
type QueueTests() =

    let guid() = Guid.NewGuid().ToString("n")
    let queue(name) = Queue.connect "localhost" ("", name)

    let event() =
        [| "text", JsonValue.String (guid()) |]
        |> JsonValue.Record
        |> Event.create
        |> Event.map JsonValue.toBytes

    [<Test; Timeout(15000)>]
    member x.TestWriteAndSubscribe() =
        let name = guid()
        let queue, event = queue name, event()

        do writer queue event
        let result =
            subscribe queue
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        Assert.AreEqual(name |> Some, result |> Event.stream)
        Assert.True(result |> Event.createdAt |> Option.isSome)

    [<Test; Timeout(15000)>]
    member x.TestWriteAndSubscribeMany() =
        let name = guid()
        let queue = queue name

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
            Assert.AreEqual(name |> Some, after |> Event.stream)
            Assert.True(after |> Event.createdAt |> Option.isSome)