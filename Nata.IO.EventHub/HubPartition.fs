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
        let group = hub.GetDefaultConsumerGroup()
        Partition.toString
        >> Receiver.toSeq None group None

    let subscribeFrom (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        Partition.toString >> fun partition ->
            Some >> fun index ->
                Receiver.toSeqWithIndex None group index partition

    let read (wait:TimeSpan) (hub:Hub) =
        Partition.toString
        >> Receiver.toSeq (Some wait) (hub.GetDefaultConsumerGroup()) None

    let readFrom (wait:TimeSpan) (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        Partition.toString >> fun partition ->
            Some >> fun index ->
                Receiver.toSeqWithIndex (Some wait) group index partition

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