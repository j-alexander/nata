namespace Nata.Service.Tests

open System
open System.Collections.Concurrent
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.Capability
open Nata.IO.Memory
open Nata.Service

[<TestFixture>]
type HubTests() =

    let connect() =
        let channel =
            Stream.connect()
            <| guid()
        channel
        |> Channel.writer,
        channel
        |> Channel.subscriberFrom
        |> Hub.Snapshot.create

    [<Test>]
    member x.TestLastSnapshot() =
        let write, hub = connect()

        let complete =
            Async.StartAsTask <| async {
                hub()
                |> Seq.map Event.data
                |> Seq.takeWhile ((<>) 10)
                |> Seq.iter ignore
            }

        seq { 1..10 }
        |> Seq.map Event.create
        |> Seq.iter write

        complete.Wait()

        for _ in 1..1000 do
            Assert.AreEqual(
                10,
                hub()
                |> Seq.map Event.data
                |> Seq.head)

    [<Test>]
    member x.TestFullSnapshotSeq() =
        let write, hub = connect()

        let queue = new BlockingCollection<int>()
        let complete =
            Async.StartAsTask <| async {
                hub()
                |> Seq.map Event.data
                |> Seq.take 1000
                |> Seq.iter queue.Add
            }

        seq { 1..1000 }
        |> Seq.log (Event.create >> write)
        |> Seq.iter(fun i ->
            Assert.AreEqual(i, queue.Take()))

        complete.Wait()
