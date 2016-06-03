namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

module HubPartition =

    let positionOf = Hub.positionOf

    let write (hub:Hub) =
        Partition.toString
        >> hub.CreatePartitionedSender
        >> fun sender (event:Event<byte[]>) -> sender.Send(new EventData(event.Data))
            
    let subscribe (hub:Hub) =
        Partition.toString
        >> hub.GetDefaultConsumerGroup().CreateReceiver
        >> Receiver.toSeq (None)

    let subscribeFrom (hub:Hub) =
        Partition.toString >> fun partition ->
            Index.toString >> fun index ->
                hub.GetDefaultConsumerGroup().CreateReceiver (partition, index)
                |> Receiver.toSeqWithIndex (None)

    let read (wait:TimeSpan) (hub:Hub) =
        Partition.toString
        >> hub.GetDefaultConsumerGroup().CreateReceiver
        >> Receiver.toSeq (Some wait)

    let readFrom (wait:TimeSpan) (hub:Hub) =
        Partition.toString >> fun partition ->
            Index.toString >> fun index ->
                hub.GetDefaultConsumerGroup().CreateReceiver(partition, index)
                |> Receiver.toSeqWithIndex (Some wait)

    let readFromPosition (wait:TimeSpan) (hub:Hub) =
        let readFrom = readFrom wait hub
        let positionOf = positionOf hub
        fun (partition:Partition) ->
            positionOf partition >> readFrom partition

    let subscribeFromPosition (hub:Hub) =
        let subscribeFrom = subscribeFrom hub
        let positionOf = positionOf hub
        fun (partition:Partition) ->
            positionOf partition >> subscribeFrom partition

    let connect : Connector<Settings,Partition,byte[],Index> =
        fun settings ->
        
            let hub, wait =
                settings |> Hub.create,
                settings.MaximumWaitTimeOnRead

            fun partition ->
                [
                    Nata.IO.Reader <| fun () ->
                        read wait hub partition

                    Nata.IO.ReaderFrom <|
                        readFromPosition wait hub partition
                
                    Nata.IO.Writer <|
                        write hub partition

                    Nata.IO.Subscriber <| fun () ->
                        subscribe hub partition

                    Nata.IO.SubscriberFrom <|
                        subscribeFromPosition hub partition
                ]           