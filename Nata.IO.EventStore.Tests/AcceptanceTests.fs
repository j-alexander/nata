namespace Nata.IO.EventStore.Tests

open System
open System.IO
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability
open Nata.IO.EventStore
open Nata.IO.EventStore.Stream

module AcceptanceTests =


    let settings : Settings =
        { Server = { Host = "localhost"
                     Port = 1113 }
          User = { Name = "admin"
                   Password = "changeit" } }
                   
    let channel() = Guid.NewGuid().ToString("n")
    let connect() = 
        Stream.connect settings
        |> Source.mapIndex (int, int64)

    [<TestFixture>]
    type EventStoreChannelTests() =
        inherit Nata.IO.Tests.ChannelTests()
        override x.Connect() = connect()
        override x.Channel() = channel()

    [<TestFixture>]
    type EventStoreSerializationTests() =
        inherit Nata.IO.Tests.SerializationTests()
        override x.Connect() = connect()
        override x.Channel() = channel()