namespace Nata.IO.EventHub.Tests

open System
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability
open Nata.IO.EventHub

[<TestFixture(Description="EventHub-Partition")>]
type PartitionTests() = 

    let settings = {
        Connection = @"Endpoint=sb://;SharedAccessKeyName=;SharedAccessKey=;EntityPath="
    }

    [<Test; Timeout(30000); Ignore("No emulator exists for EventHub")>]
    member x.TestWriteSubscribe() =
        let hub = Hub.create settings

        let connectTo =
            Partition.connect hub
            |> Source.mapData Codec.BytesToString
            
        let write, subscribe =
            let partitions = Hub.partitions hub
            let partition = connectTo partitions.[4]
            writer partition, subscriber partition

        let event = guid() |> Event.create
        do write event

        let result =
            subscribe()
            |> Seq.filter (Event.data >> (=) event.Data)
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
