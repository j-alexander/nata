namespace Nata.IO.EventHub.Tests

open System
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability
open Nata.IO.EventHub

[<TestFixture(Description="EventHub-Hub"); Ignore("No emulator exists for EventHub")>]
type HubTests() = 

    let settings = {
        Connection = @"Endpoint=sb://;SharedAccessKeyName=;SharedAccessKey=;EntityPath="
        MaximumWaitTimeOnRead = TimeSpan.FromSeconds(10.0)
    }

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