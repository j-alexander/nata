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
    }

    [<Test; Timeout(30000)>]
    member x.TestWriteSubscribe() =

        let write, subscribe =
            let connect =
                settings
                |> Hub.create
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