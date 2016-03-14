namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
open KafkaNet.Protocol
    
type Offset =
    { PartitionId : int
      Topic : string
      Min : int64
      Max : int64 }

type Offsets = Offset list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partitionId (x:Offset) = x.PartitionId
    let topic (x:Offset) = x.Topic
    let max (x:Offset) = x.Max
    let min (x:Offset) = x.Min

    let fromResponse (x:OffsetResponse) =
        let offsets =
            match x.Error with
            | 0s -> [ yield! x.Offsets ]
            | _  -> [ 0L ]
        { Offset.Topic = x.Topic
          Offset.PartitionId = x.PartitionId
          Offset.Min = Seq.last offsets
          Offset.Max = Seq.head offsets }

    let toPosition (x:Offset) =
        new OffsetPosition(x.PartitionId, x.Min)
        