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
            let partition = connectTo partitions.[0]
            writer partition, subscriber partition

        let event = guid() |> Event.create
        do write event

        let result =
            subscribe()
            |> Seq.filter (Event.data >> (=) event.Data)
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        
    [<Test; Timeout(30000); Ignore("No emulator exists for EventHub")>]
    member x.TestPartitionEventIsolation() =
        let hub = Hub.create settings

        let connectTo =
            Partition.connect hub
            |> Source.mapData Codec.BytesToString
            
        let partitions = Hub.partitions hub
        let subscribe, flush =
            subscriber <| connectTo partitions.[1],
            writer <| connectTo partitions.[1]

        let unexpected = guid() |> Event.create
        for write in partitions
                     |> Seq.filter ((<>) partitions.[1])
                     |> Seq.map (connectTo >> writer) do
            write unexpected
        
        let expected = guid() |> Event.create
        do flush expected

        let results =
            subscribe()
            |> Seq.map Event.data
            |> Seq.takeWhile ((<>) expected.Data)
            |> Seq.filter ((=) unexpected.Data)
            |> Seq.toList

        Assert.AreEqual([], results)