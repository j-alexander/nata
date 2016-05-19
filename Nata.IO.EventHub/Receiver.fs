namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeqWithOffset (wait:TimeSpan option) (receiver:Receiver) =

        let partition =
            receiver.PartitionId
            |> Partition.parse
        let receive() =
            match wait with
            | Some max -> receiver.Receive(max)
            | None -> receiver.Receive()

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

    let toSeqWithIndex wait =
        toSeqWithOffset wait >> Seq.mapSnd Offset.index

    let toSeq wait =
        toSeqWithOffset wait >> Seq.map fst
