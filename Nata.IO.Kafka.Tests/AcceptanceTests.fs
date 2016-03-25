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

    let cluster = Cluster.connect "tcp://127.0.0.1:9092"

    // to unit test on a local kafka instance, the following broker
    // settings are required (in ./config/server.properties):
    //
    // num.partitions=1
    // auto.create.topics.enable=true

    [<TestFixture>]
    type KafkaChannelTests() =
        inherit Nata.IO.Tests.ChannelTests()

        override x.Connect() =
            Cluster.topics cluster
            |> Source.mapIndex (Offsets.Codec.Int64ToOffsets 0)
            
        override x.Channel() =
             Guid.NewGuid().ToString("n")