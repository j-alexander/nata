namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Partition =

    let write (hub:Hub) (partition:string) =
        let sender = hub.CreatePartitionedSender(partition)
        fun (event:Event<byte[]>) ->
            sender.Send(new EventData(event.Data))
            
    let subscribe (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        group.CreateReceiver >> Receiver.toSeq

    let connect : Connector<Hub,Partition,byte[],unit> =
        fun hub partition ->
            [
                Nata.IO.Writer <|
                    write hub (partition.ToString())

                Nata.IO.Subscriber <| fun () ->
                    subscribe hub (partition.ToString())
            ]           