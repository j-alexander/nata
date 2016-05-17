namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Partition = int

type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeq (receiver:Receiver) =
        let partition =
            receiver.PartitionId
            |> Int32.Parse
        Seq.initInfinite <| fun _ ->
            let data = receiver.Receive()
            data.GetBytes()
            |> Event.create
            |> Event.withPartition partition
            |> Event.withSentAt data.EnqueuedTimeUtc