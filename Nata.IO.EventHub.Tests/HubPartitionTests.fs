namespace Nata.IO.EventHub.Tests

open System
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.EventHub

[<TestFixture(Description="EventHub-Partition"); Ignore("No emulator exists for EventHub")>]
type HubPartitionTests() = 
    inherit Nata.IO.Tests.LogStoreTests()

    let channel = Partition.toString 0
    let settings = {
        Connection = @"Endpoint=sb://;SharedAccessKeyName=;SharedAccessKey=;EntityPath="
        MaximumWaitTimeOnRead = TimeSpan.FromSeconds(10.0)
    }

    override x.Connect() =
        HubPartition.connect settings
        |> Source.mapChannel Partition.Codec.PartitionToString
        |> Source.mapCapabilities (MaskEnvelope.mapCapability (guid()))
        <| channel

    [<Test; Timeout(30000)>]
    member x.TestReadNone() =

        let connectTo =
            HubPartition.connect settings
            |> Source.mapData Codec.BytesToString
            
        let partition =
            connectTo 0

        let results =
            read partition
            |> Seq.filter (fun _ -> false)
            |> Seq.toList

        Assert.AreEqual([], results)

    [<Test; Timeout(30000)>]
    member x.TestWriteRead() =

        let connectTo =
            HubPartition.connect settings
            |> Source.mapData Codec.BytesToString
            
        let write, read, subscribe =
            let partition = connectTo 0
            writer partition,
            reader partition,
            subscriber partition

        let event = guid() |> Event.create
        do write event

        let flush = guid() |> Event.create
        do write flush
        subscribe()
        |> Seq.takeWhile (Event.data >> (<>) flush.Data)
        |> Seq.iter ignore

        let results =
            read()
            |> Seq.filter (Event.data >> (=) event.Data)
            |> Seq.map Event.data
            |> Seq.toList

        Assert.AreEqual([ event.Data ], results)

    [<Test; Timeout(30000)>]
    member x.TestWriteSubscribe() =

        let connectTo =
            HubPartition.connect settings
            |> Source.mapData Codec.BytesToString
            
        let write, subscribe =
            let partition = connectTo 0
            writer partition, subscriber partition

        let event = guid() |> Event.create
        do write event

        let result =
            subscribe()
            |> Seq.filter (Event.data >> (=) event.Data)
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        
    [<Test; Timeout(60000)>]
    member x.TestPartitionEventIsolation() =

        let partitions =
            settings 
            |> Hub.create
            |> Hub.partitions

        let connectTo =
            HubPartition.connect settings
            |> Source.mapData Codec.BytesToString
            
        let subscribe, flush =
            let partition = connectTo 1
            subscriber <| partition,
            writer <| partition

        let unexpected = guid() |> Event.create
        for write in partitions
                     |> Seq.filter ((<>) 1)
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