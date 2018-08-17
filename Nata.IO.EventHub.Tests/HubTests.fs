namespace Nata.IO.EventHub.Tests

open System
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.EventHub

[<TestFixture(Description="EventHub-Hub"); Ignore("No emulator exists for EventHub")>]
type HubTests() = 
    inherit Nata.IO.Tests.LogStoreTests()

    let channel = ""
    let settings = {
        Connection = @"Endpoint=sb://;SharedAccessKeyName=;SharedAccessKey=;EntityPath="
        MaximumMessageCountOnRead = 1024
        MaximumWaitTimeOnRead = TimeSpan.FromSeconds(10.0)
    }

    override x.Connect() =
        let toPartition p i = [{Partition=p; Index=i}]
        let ofPartition p = Offsets.partition p >> Offset.index
        let onPartition p = function
            | Writer x -> Event.withPartition p >> x |> Writer 
            | x -> x
        Hub.connect settings
        |> Source.mapChannel ((fun _ -> ""), ignore)
        |> Source.mapIndex (ofPartition 0, toPartition 0)
        |> Source.mapCapabilities (MaskEnvelope.mapCapability (guid()) >> onPartition 0)
        <| channel

    [<Test; Timeout(30000)>]
    member x.TestWriteSubscribe() =

        let write, subscribe =
            let connect =
                settings
                |> Hub.connect
                |> Source.mapData Codec.BytesToString
            let hub = connect()
            writer hub, subscriber hub

        let event = guid() |> Event.create
        do write event

        let result =
            subscribe()
            |> Seq.filter (Event.data >> (=) event.Data)
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
  
    [<Test; Timeout(60000)>]
    member x.TestWriteRead() =
        let connect =
            Hub.connect settings
            |> Source.mapData Codec.BytesToString
            
        let write, read, subscribe =
            let connection = connect()
            writer connection,
            reader connection,
            subscriber connection

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
        
    [<Test>]
    override x.TestReadFromRelativePositions() = Assert.Ignore("Not yet supported.")
    [<Test>]
    override x.TestReadFromBeforeEndAsSnapshot() = Assert.Ignore("Not yet supported.")
    [<Test>]
    override x.TestSubscribeFromRelativePositions() = Assert.Ignore("Not yet supported.")