namespace Nata.IO.Kafka.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability
open Nata.IO.Kafka

module AcceptanceTests =

    let x = 3
//    [<TestFixture>]
//    type KafkaChannelTests() =
//        inherit Nata.IO.Tests.ChannelTests()
//        override x.Connect() = connect()
//        override x.Channel() = channel()
//
//    [<TestFixture>]
//    type KafkaSerializationTests() =
//        inherit Nata.IO.Tests.SerializationTests()
//        override x.Connect() = connect()
//        override x.Channel() = channel()