namespace Nata.IO.Kafka

open System
open Kafunk

    
type OffsetRange =
    { Topic : Topic
      PartitionId : Partition
      Min : Index
      Max : Index }

type OffsetRanges = OffsetRange list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OffsetRange =

    let topic (x:OffsetRange) = x.Topic
    let partitionId (x:OffsetRange) = x.PartitionId
    let min (x:OffsetRange) = x.Min
    let max (x:OffsetRange) = x.Max

    let queryAllAsync { Cluster=cluster } topic =
        async {
            let! offsets = Offsets.offsetRange cluster topic Array.empty
            return
                [
                    for partition, (min, max) in Map.toSeq offsets ->
                        { Topic=topic; PartitionId=partition; Min=min; Max=max }
                ]
        }

    let queryAll connection topic =
        queryAllAsync connection topic
        |> Async.RunSynchronously

    let queryAsync { Cluster=cluster } topic partition =
        async {
            let! offsets = Offsets.offsetRange cluster topic [| partition |]
            return
                match Map.toList offsets with
                | [ partition, (min, max) ] ->
                    Some { Topic=topic; PartitionId=partition; Min=min; Max=max }
                | _ ->
                    None
        }

    let query connection topic partition =
        queryAsync connection topic partition
        |> Async.RunSynchronously