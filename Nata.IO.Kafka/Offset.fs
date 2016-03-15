namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
    
type Offset =
    { PartitionId : int 
      Position : int64 }

type Offsets = Offset list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partitionId (x:Offset) = x.PartitionId
    let position (x:Offset) = x.Position

    let start (partition:Partition) =
        { Offset.PartitionId = partition.Id
          Offset.Position = partition.Min }

    let toOffsetPosition (x:Offset) =
        new KafkaNet.Protocol.OffsetPosition(x.PartitionId, x.Position)