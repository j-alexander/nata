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
        let partitions = Hub.partitions hub
        let partition = Partition.connect hub partitions.[0]

        let write, subscribe = writer partition, subscriber partition

        let message = Guid.NewGuid().ToByteArray()
        let event = Event.create message

        do write event
        let result = subscribe() |> Seq.head

        Assert.AreEqual(message, result.Data)
