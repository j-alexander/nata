namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeq (receiver:Receiver) =
        Seq.initInfinite <| fun _ ->
            let data = receiver.Receive()
            data.GetBytes()
            |> Event.create
            //|> Event.withPartition receiver.PartitionId
            |> Event.withSentAt data.EnqueuedTimeUtc