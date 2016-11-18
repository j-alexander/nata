namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

open Nata.Core
open Nata.IO

type Cluster = string list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Cluster =
    
    let private delay = TimeSpan.FromMilliseconds(500.)

    let private connect(cluster) =
        new BrokerRouter(
            new KafkaOptions(
                [| for x in cluster -> new Uri(x)|]))

    let private topicFor (cluster:Cluster) (name:TopicName) =
        { Topic.Consumer =
            fun () ->
                new Consumer(
                    new ConsumerOptions(
                        name,
                        connect cluster,
                        MaxWaitTimeForMinimumBytes=delay))
          Topic.Producer =
            fun () ->
                new Producer(
                    connect cluster,
                    BatchDelayTime=delay)
          Topic.Name = name }

    let private topicPartitionFor (cluster:Cluster) (name:TopicName) (partitionId:int) =
        { TopicPartition.Topic = topicFor cluster name
          TopicPartition.Partition = partitionId }

    let topics : Connector<Cluster,TopicName,Data,Offsets>  =
        fun cluster name ->
            [
                Capability.Indexer <|
                    (Topic.index (topicFor cluster name))

                Capability.Reader <| fun () ->
                    (Topic.read (topicFor cluster name) |> Seq.map (Event.ofMessage name))

                Capability.ReaderFrom <|
                    (Topic.readFrom (topicFor cluster name) >> Seq.mapFst (Event.ofMessage name))

                Capability.Writer <|
                    (Event.toMessage >> Topic.write (topicFor cluster name))

                Capability.Subscriber <| fun () ->
                    (Topic.listen (topicFor cluster name) |> Seq.map (Event.ofMessage name))

                Capability.SubscriberFrom <|
                    (Topic.listenFrom (topicFor cluster name) >> Seq.mapFst (Event.ofMessage name))
            ]

    let partitions : Connector<Cluster,TopicName*int,Data,Offset> =
        fun cluster (name,partition) ->
            [
                Capability.Indexer <|
                    (TopicPartition.index (topicPartitionFor cluster name partition))

                Capability.Reader <| fun () ->
                    (TopicPartition.read (topicPartitionFor cluster name partition) |> Seq.map (Event.ofMessage name))

                Capability.ReaderFrom <|
                    (TopicPartition.readFrom (topicPartitionFor cluster name partition) >> Seq.mapFst (Event.ofMessage name))

                Capability.Writer <|
                    (Event.toMessage >> TopicPartition.write (topicPartitionFor cluster name partition))

                Capability.Subscriber <| fun () ->
                    (TopicPartition.listen (topicPartitionFor cluster name partition) |> Seq.map (Event.ofMessage name))

                Capability.SubscriberFrom <|
                    (TopicPartition.listenFrom (topicPartitionFor cluster name partition) >> Seq.mapFst (Event.ofMessage name))
            ]