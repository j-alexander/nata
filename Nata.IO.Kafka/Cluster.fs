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
    
    let connect (host:string) : Cluster =
        new BrokerRouter(new KafkaOptions(new Uri(host)))

    let topicFor (cluster:Cluster) (name:TopicName) =
        { Topic.Consumer = new Consumer(new ConsumerOptions(name, cluster))
          Topic.Producer = new Producer(cluster)
          Topic.Name = name }

    let topics : Nata.IO.Connector<Cluster,TopicName,Data,Metadata,Offsets>  =
        fun cluster name ->
            [
                Nata.IO.Capability.Reader <| fun () ->
                    (Topic.read (topicFor cluster name) |> Seq.map (Event.ofMessage name))

                Nata.IO.Capability.ReaderFrom <|
                    (Topic.readFrom (topicFor cluster name) >> Seq.mapFst (Event.ofMessage name))

                Nata.IO.Capability.Writer <|
                    (Event.toMessage 0 0L >> Topic.write (topicFor cluster name))

                Nata.IO.Capability.Subscriber <| fun () ->
                    (Topic.listen (topicFor cluster name) |> Seq.map (Event.ofMessage name))

                Nata.IO.Capability.SubscriberFrom <|
                    (Topic.listenFrom (topicFor cluster name) >> Seq.mapFst (Event.ofMessage name))
            ]
            
        
        