namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Partition = int

type Receiver = EventHubReceiver

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Receiver =

    let toSeq (wait:TimeSpan option) (receiver:Receiver) =

        let partition =
            receiver.PartitionId
            |> Int32.Parse
        let receive() =
            match wait with
            | Some max -> receiver.Receive(max)
            | None -> receiver.Receive()

        Seq.unfold(receive >> function null -> None | x -> Some(x,())) ()
        |> Seq.map(fun data ->
            data.GetBytes()
            |> Event.create
            |> Event.withPartition partition
            |> Event.withSentAt data.EnqueuedTimeUtc)
