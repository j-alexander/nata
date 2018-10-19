namespace Nata.IO.KafkaNet

open System
open System.Net
open System.Text
open KafkaNet
open KafkaNet.Model
    
type OffsetRange =
    { Topic : string
      PartitionId : int
      Min : int64
      Max : int64 }

type OffsetRanges = OffsetRange list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OffsetRange =

    let topic (x:OffsetRange) = x.Topic
    let partitionId (x:OffsetRange) = x.PartitionId
    let min (x:OffsetRange) = x.Min
    let max (x:OffsetRange) = x.Max

    let fromKafka (x:KafkaNet.Protocol.OffsetResponse) =
        let offsets =
            match x.Error with
            | 0s -> [ yield! x.Offsets ]
            | _  -> [ 0L ]
        { OffsetRange.Topic = x.Topic
          OffsetRange.PartitionId = x.PartitionId
          OffsetRange.Min = Seq.last offsets
          OffsetRange.Max = Seq.head offsets }