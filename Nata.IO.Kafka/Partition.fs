namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
    
type Partition =
    { Topic : string
      Id : int
      Min : int64
      Max : int64 }

type Partitions = Partition list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Partition =

    let topic (x:Partition) = x.Topic
    let id (x:Partition) = x.Id
    let min (x:Partition) = x.Min
    let max (x:Partition) = x.Max

    let fromOffsetResponse (x:KafkaNet.Protocol.OffsetResponse) =
        let offsets =
            match x.Error with
            | 0s -> [ yield! x.Offsets ]
            | _  -> [ 0L ]
        { Partition.Topic = x.Topic
          Partition.Id = x.PartitionId
          Partition.Min = Seq.last offsets
          Partition.Max = Seq.head offsets }
        