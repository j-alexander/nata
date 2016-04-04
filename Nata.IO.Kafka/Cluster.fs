namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

open Nata.IO

type Cluster = BrokerRouter

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Cluster =
    
    let delay = TimeSpan.FromMilliseconds(500.)

    let connect (host:string) : Cluster =
        new BrokerRouter(new KafkaOptions(new Uri(host)))

    let topicFor (cluster:Cluster) (name:TopicName) =
        { Topic.Consumer =
            new Consumer(
                new ConsumerOptions(
                    name,
                    cluster,
                    MaxWaitTimeForMinimumBytes=delay))
          Topic.Producer =
            new Producer(
                cluster,
                BatchDelayTime=delay)
          Topic.Name = name }

    let topics : Connector<Cluster,TopicName,Data,Offsets>  =
        fun cluster name ->
            [
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
            
        
        