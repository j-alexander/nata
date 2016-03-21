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

    let start (range:OffsetRange) =
        { Offset.PartitionId = range.PartitionId
          Offset.Position = range.Min }

    let remaining (range:OffsetRange, offset:Offset) =
        range.Max - Math.Min(offset.Position, range.Max)

    let completed (range:OffsetRange, offset:Offset) =
        remaining (range, offset) <= 0L

    let toKafka (x:Offset) =
        new KafkaNet.Protocol.OffsetPosition(x.PartitionId, x.Position)
        
    let fromKafka (x:KafkaNet.Protocol.ProduceResponse) =
        if (x.Error <> 0s) then
            x.Error |> sprintf "Kafka error response: %d" |> failwith
        else
            { Offset.PartitionId  = x.PartitionId
              Offset.Position = x.Offset }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offsets =

    let private join (ranges:OffsetRanges, offsets:Offsets) =
        query { for p in ranges do
                for o in offsets do
                where (p.PartitionId = o.PartitionId)
                select (p, o) }
        |> Seq.toList

    let remaining = join >> List.map Offset.remaining >> List.sum

    let completed = join >> List.forall Offset.completed

    let neverCompleted _ = false

    let start : OffsetRanges -> Offsets =
        List.map Offset.start

    let updateWith (message:Message) : Offsets -> Offsets =
        List.map(fun offset ->
            if offset.PartitionId <> message.PartitionId then offset
            else { offset with Position = 1L + message.Offset })

    let toKafka : Offsets -> KafkaNet.Protocol.OffsetPosition[] =
        Seq.sortBy Offset.partitionId
        >> Seq.map Offset.toKafka
        >> Seq.toArray