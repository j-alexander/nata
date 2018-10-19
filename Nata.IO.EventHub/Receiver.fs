namespace Nata.IO.EventHub

open System
open Microsoft.Azure.EventHubs
open Nata.Core
open Nata.IO

type Group = string
type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeqWithOffset (wait:TimeSpan option)
                        (hub:EventHubClient)
                        (startAt:Index option)
                        (partition:PartitionString) =
        seq {
            let group =
                PartitionReceiver.DefaultConsumerGroupName

            let receiver =
                match startAt with
                | None -> hub.CreateReceiver(group, partition, EventPosition.FromStart())
                | Some start -> hub.CreateReceiver(group, partition, EventPosition.FromOffset(Index.toString (start-1L)))

            let wait =
                wait |> Option.defaultValue TimeSpan.MaxValue
            let receive _ =
                receiver.ReceiveAsync(1, wait) |> Task.waitForResult

            use connection =
                { new IDisposable with
                    member x.Dispose() = receiver.Close() }
                    
            let partition = Partition.parse partition

            yield!
                Seq.unfold(receive >> function null -> None | x -> Some(x,())) ()
                |> Seq.collect id
                |> Seq.map(fun data ->
                    let index = 
                        data.SystemProperties.Offset
                        |> Index.parse
                    data.Body.Array
                    |> Event.create
                    |> Event.withPartition partition
                    |> Event.withIndex index
                    |> Event.withSentAt data.SystemProperties.EnqueuedTimeUtc,
                    { Offset.Partition = partition
                      Offset.Index = index })
        }

    let toSeqWithIndex wait hub startAt partition =
        toSeqWithOffset wait hub startAt partition
        |> Seq.mapSnd Offset.index

    let toSeq wait hub startAt partition =
        toSeqWithOffset wait hub startAt partition
        |> Seq.map fst
