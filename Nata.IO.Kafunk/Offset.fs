namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open Kafunk

open Nata.Core
open Nata.IO
    
type Offset =
    { PartitionId : Partition 
      Position : Index }
with
    static member (+) (o:Offset, delta) : Offset = { o with Position=o.Position+delta }
    static member (-) (o:Offset, delta) : Offset = { o with Position=o.Position-delta }

type Offsets = Offsets of Offset list
with
    static member (+) (Offsets offsets, deltaPerPartition) : Offsets =
        offsets
        |> List.map (fun x -> x + deltaPerPartition)
        |> Offsets

    static member (-) (Offsets offsets, deltaPerPartition) : Offsets =
        offsets
        |> List.map (fun x -> x - deltaPerPartition)
        |> Offsets

    static member (+) (offsets:Offsets, deltaPerPartition:int) =
        Offsets.op_Addition(offsets, int64 deltaPerPartition)

    static member (-) (offsets:Offsets, deltaPerPartition:int) =
        Offsets.op_Subtraction(offsets, int64 deltaPerPartition)

    static member (-) (Offsets l, Offsets r) =
        query {
            for l in l do
            join r in r on (l.PartitionId = r.PartitionId)
            sumBy (l.Position - Math.Min(l.Position, r.Position))
        }
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partitionId (x:Offset) = x.PartitionId
    let position (x:Offset) = x.Position

    let start (range:OffsetRange) =
        { Offset.PartitionId = range.PartitionId
          Offset.Position = range.Min }

    let finish (range:OffsetRange) =
        { Offset.PartitionId = range.PartitionId
          Offset.Position = range.Max }

    let remaining (range:OffsetRange, offset:Offset) =
        range.Max - Math.Min(offset.Position, range.Max)

    let completed (range:OffsetRange, offset:Offset) =
        remaining (range, offset) <= 0L
              
    let (|Offset|_|) =
        String.split '@' >> function
        | [ Integer32 p; Integer64 o ] -> Some { PartitionId=p; Position=o }
        | _ -> None

    let ofInt64 (partitionId:int) (position:int64) = { PartitionId=partitionId; Position=position }
    let toInt64 = position

    let toString (o:Offset) = sprintf "%d@%d" o.PartitionId o.Position
    let ofString = (|Offset|_|) >> Option.get

    module Codec =

        let OffsetToInt64 partition : Codec<Offset,int64> = toInt64, ofInt64 partition
        let Int64ToOffset partition : Codec<int64,Offset> = ofInt64 partition, toInt64
        
        let OffsetToString : Codec<Offset,string> = toString, ofString
        let StringToOffset : Codec<string,Offset> = ofString, toString


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offsets =

    let private join (ranges:OffsetRanges, Offsets offsets) =
        query { for p in ranges do
                for o in offsets do
                where (p.PartitionId = o.PartitionId)
                select (p, o) }
        |> Seq.toList

    let remaining = join >> List.map Offset.remaining >> List.sum

    let completed = join >> List.forall Offset.completed

    let neverCompleted _ = false

    let start : OffsetRanges -> Offsets =
        List.map Offset.start >> Offsets

    let finish : OffsetRanges -> Offsets =
        List.map Offset.finish >> Offsets

    let before (range:OffsetRanges) (offsets:Offsets) =
        raise (new NotImplementedException())

    let after (range:OffsetRanges) (offsets:Offsets) =
        raise (new NotImplementedException())

    let filter (partition:int) (Offsets offsets) =
        offsets
        |> List.filter (Offset.partitionId >> (=) partition)
        |> Offsets

    let partitions (Offsets offsets) =
        offsets
        |> List.map Offset.partitionId

    let position (partition:int) (Offsets offsets) =
        offsets
        |> Seq.filter (Offset.partitionId >> (=) partition)
        |> Seq.map Offset.position
        |> Seq.head

    let toInt64 (partition) (Offsets offsets) : int64 =
        offsets
        |> List.filter (Offset.partitionId >> (=) partition)
        |> List.map Offset.position
        |> List.head

    let ofInt64 (partition) (position:int64) : Offsets =
        [ Offset.ofInt64 partition position ]
        |> Offsets

    let toString (Offsets offsets) : string =
        offsets
        |> List.sortBy Offset.partitionId 
        |> List.map Offset.toString 
        |> String.concat ","

    let ofString : string -> Offsets =
        String.split ',' 
        >> List.choose Offset.(|Offset|_|) 
        >> List.sortBy Offset.partitionId
        >> Offsets

    module Codec =
        
        let OffsetsToInt64 partition : Codec<Offsets,int64> = toInt64 partition, ofInt64 partition
        let Int64ToOffsets partition : Codec<int64,Offsets> = ofInt64 partition, toInt64 partition

        let OffsetsToString : Codec<Offsets,string> = toString, ofString
        let StringToOffsets : Codec<string,Offsets> = ofString, toString