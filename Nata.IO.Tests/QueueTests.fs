namespace Nata.IO.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.Core
open Nata.Core.JsonValue.Codec
open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type QueueTests<'Channel>() =

    let event() =
        [| "text", JsonValue.String (guid()) |]
        |> JsonValue.Record
        |> Event.create
        |> Event.map JsonValue.toBytes

    abstract member Connect : unit -> Source<'Channel,byte[],int64>
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

    [<Test;Timeout(25000)>]
    member x.TestWriteIncreasesIndex() =
        let name = x.Channel()
        let queue = name |> x.Connect()

        match tryIndexer queue with
        | None ->
            Assert.Ignore("Indexer is reported to be unsupported by this source.")
        | Some indexOf ->
            let range() = 
                indexOf Position.Start,
                indexOf Position.End
            Assert.AreEqual((0L,0L), range())

            for numberOfMessagesInQueue in 1L..5L do
                event() |> writer queue
                // wait for the range to be updated in the queue
                while range() <> (0L,numberOfMessagesInQueue) do Thread.Sleep(100)

            for numberOfMessagesInQueue in 4L..0L do
                let event = subscriber queue () |> Seq.head
                // wait for the range to be updated in the queue
                while range() <> (0L,numberOfMessagesInQueue) do Thread.Sleep(100)


            Assert.Pass("Adding a new message increases Position.End.")
