namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
open KafkaNet.Protocol

type Cluster = BrokerRouter

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Cluster =
    
    let connect (host:string) : Cluster =
        new BrokerRouter(new KafkaOptions(new Uri(host)))


        