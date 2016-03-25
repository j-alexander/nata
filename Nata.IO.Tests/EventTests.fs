namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<TestFixture>]
type EventTests() =

    let date = DateTime.UtcNow
    let event =
        Event.createAt date "0"
        |> Event.withEventType "event_type"
        |> Event.withStream "event_stream"
        |> Event.withName "event_name"

    [<Test>]
    member x.TypeTest() =
        Assert.AreEqual(Some "event_type", Event.eventType event)

    [<Test>]
    member x.StreamTest() =
        Assert.AreEqual(Some "event_stream", Event.stream event)

    [<Test>]
    member x.DateTest() =
        Assert.AreEqual(date, Event.at event)

    [<Test>]
    member x.DataTest() =
        Assert.AreEqual("0", Event.data event)

    [<Test>]
    member x.CreationTest() =
        let expected =
            { Event.Data = "0"
              Event.Metadata = [ Value.Name "event_name"
                                 Value.Stream "event_stream"
                                 Value.EventType "event_type" ]
              Event.At = date }
        Assert.AreEqual(expected, event)

    [<Test>]
    member x.MapTypeTest() =
        let expected =
            { Event.Data = 0
              Event.Metadata = [ Value.Name "event_name"
                                 Value.Stream "event_stream"
                                 Value.EventType "event_type" ]
              Event.At = date }
        Assert.AreEqual(expected, event |> Event.map Int32.Parse)

    [<Test>]
    member x.MapValueTest() =
        let expected =
            { Event.Data = "2"
              Event.Metadata = [ Value.Name "event_name"
                                 Value.Stream "event_stream"
                                 Value.EventType "event_type" ]
              Event.At = date }
        Assert.AreEqual(expected, event |> Event.map (function "0" -> "2" | _ -> "3"))