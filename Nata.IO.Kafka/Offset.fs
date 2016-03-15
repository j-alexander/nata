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

    let remaining (partition:Partition, offset:Offset) =
        partition.Max - Math.Min(offset.Position, partition.Max)

    let completed (partition:Partition, offset:Offset) =
        remaining (partition, offset) <= 0L

    let toOffsetPosition (x:Offset) =
        new KafkaNet.Protocol.OffsetPosition(x.PartitionId, x.Position)
        

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offsets =

    let private join (partitions:Partitions, offsets:Offsets) =
        query { for p in partitions do
                for o in offsets do
                where (p.Id = o.PartitionId)
                select (p, o) }
        |> Seq.toList

    let remaining = join >> List.map Offset.remaining >> List.sum

    let completed = join >> List.forall Offset.completed

    let neverCompleted _ = false

    let start : Partitions -> Offsets =
        List.map Offset.start

    let updateWith (message:Message) : Offsets -> Offsets =
        List.map(fun offset ->
            if offset.PartitionId <> message.PartitionId then offset
            else { offset with Position = 1L + message.Offset })

    let toOffsetPosition : Offsets -> KafkaNet.Protocol.OffsetPosition[] =
        Seq.sortBy Offset.partitionId
        >> Seq.map Offset.toOffsetPosition
        >> Seq.toArray
