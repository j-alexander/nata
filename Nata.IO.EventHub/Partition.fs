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
        group.CreateReceiver >> Receiver.toSeq (None)

    let read (wait:TimeSpan) (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        group.CreateReceiver >> Receiver.toSeq (Some wait)

    let connect : Connector<Settings,Partition,byte[],unit> =

        fun settings ->

            let hub = settings |> Hub.create 
            let wait = settings.MaximumWaitTimeOnRead

            fun partition ->
                
                let partitionId = partition.ToString()

                [
                    Nata.IO.Reader <| fun () ->
                        read wait hub partitionId
                
                    Nata.IO.Writer <|
                        write hub partitionId

                    Nata.IO.Subscriber <| fun () ->
                        subscribe hub partitionId
                ]           