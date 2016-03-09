namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<TestFixture>]
type EventTests() =

    let date = DateTime.Now
    let event =
        { Type = "event_type"
          Stream = "event_stream"
          Date = date
          Data = "0"
          Metadata = "1" }

    [<Test>]
    member x.EventTypeTest() =
        Assert.AreEqual("event_type", event |> Event.``type``)

    [<Test>]
    member x.EventStreamTest() =
        Assert.AreEqual("event_stream", event |> Event.stream)

    [<Test>]
    member x.EventDateTest() =
        Assert.AreEqual(date, event |> Event.date)

    [<Test>]
    member x.EventDataTest() =
        Assert.AreEqual("0", event |> Event.data)

    [<Test>]
    member x.EventMetadataTest() =
        Assert.AreEqual("1", event |> Event.metadata)

    [<Test>]
    member x.MapDataTypeTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = 0
              Metadata = "1" }
        Assert.AreEqual(expected, event |> Event.mapData (Int32.Parse))

    [<Test>]
    member x.MapDataValueTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = "2"
              Metadata = "1" }
        Assert.AreEqual(expected, event |> Event.mapData (function "0" -> "2" | _ -> "3"))
        

    [<Test>]
    member x.MapMetadataTypeTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = "0"
              Metadata = 1 }
        Assert.AreEqual(expected, event |> Event.mapMetadata (Int32.Parse))

    [<Test>]
    member x.MapMetadataValueTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = "0"
              Metadata = "2" }
        Assert.AreEqual(expected, event |> Event.mapMetadata (function "1" -> "2" | _ -> "3"))

    [<Test>]
    member x.MapTypeTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = 0
              Metadata = 1 }
        Assert.AreEqual(expected, event |> Event.map (Int32.Parse) (Int32.Parse))

    [<Test>]
    member x.MapValueTest() =
        let expected = 
            { Type = "event_type"
              Stream = "event_stream"
              Date = date
              Data = "zero"
              Metadata = "one" }
        let mapper = function
            | "0" -> "zero"
            | "1" -> "one"
            | _ -> "something else"
        Assert.AreEqual(expected, event |> Event.map mapper mapper)
        