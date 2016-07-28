namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Group = EventHubConsumerGroup
type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeqWithOffset (wait:TimeSpan option)
                        (group:Group)
                        (startAt:string option)
                        (partitionId:PartitionId) =

        let partition = Partition.parse partitionId

        seq {
            let receiver =
                match startAt with
                | None | Some null -> group.CreateReceiver(partitionId)
                | Some start -> group.CreateReceiver(partitionId, start)
                
            let receive _ =
                match wait with
                | Some max -> receiver.Receive(max)
                | None -> receiver.Receive()

            use connection =
                { new IDisposable with
                    member x.Dispose() = if not receiver.IsClosed then receiver.Close() }

            yield!
                Seq.unfold(receive >> function null -> None | x -> Some(x,())) ()
                |> Seq.map(fun data ->
                    let index = 
                        data.Offset
                        |> Index.parse
                    data.GetBytes()
                    |> Event.create
                    |> Event.withPartition partition
                    |> Event.withIndex index
                    |> Event.withSentAt data.EnqueuedTimeUtc,
                    { Offset.Partition = partition
                      Offset.Index = index })
        }

    let toSeqWithIndex wait group startAt partitionId =
        toSeqWithOffset wait group startAt partitionId
        |> Seq.mapSnd Offset.index

    let toSeq wait group startAt partitionId =
        toSeqWithOffset wait group startAt partitionId
        |> Seq.map fst
