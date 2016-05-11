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
            
    let subscribe (hub:Hub) (partition:string) (consumerGroup:string) =
        let group = hub.GetConsumerGroup(consumerGroup)
        let receiver = group.CreateReceiver(partition)
        Seq.initInfinite <| fun _ ->
            let data = receiver.Receive()
            data.GetBytes()
            |> Event.create
            |> Event.withSentAt data.EnqueuedTimeUtc

    let connect : Connector<Hub,Partition,byte[],unit> =
        fun hub partition ->
            [
                Nata.IO.Writer <|
                    (write hub partition)

                Nata.IO.Subscriber <|
                    (guid >> subscribe hub partition)
            ]           