namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

type Cluster = BrokerRouter

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Cluster =
    
    let connect (host:string) : Cluster =
        new BrokerRouter(new KafkaOptions(new Uri(host)))

    let topicFor (cluster:Cluster) (name:string) : Topic =
        { Topic.Consumer = new Consumer(new ConsumerOptions(name, cluster))
          Topic.Producer = new Producer(cluster)
          Topic.Name = name }