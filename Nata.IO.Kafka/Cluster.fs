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
    
    let internal delay = TimeSpan.FromMilliseconds(500.)

    let internal connect(cluster) =
        new BrokerRouter(
            new KafkaOptions(
                [| for x in cluster -> new Uri(x)|]))