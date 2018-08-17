namespace Nata.IO.EventHub

open System
open Microsoft.Azure.EventHubs
open Nata.Core
open Nata.IO

module HubPartition =

    let positionOf = Hub.positionOf

    let write (hub:Hub) =
        Partition.toString
        >> hub.CreatePartitionSender
        >> fun sender (event:Event<byte[]>) -> sender.SendAsync(new EventData(event.Data)) |> Task.wait
            
    let subscribe (hub:Hub) =
        Partition.toString
        >> Receiver.toSeq None hub None

    let subscribeFrom (hub:Hub) =
        Partition.toString >> fun partition ->
            Some >> fun index ->
                Receiver.toSeqWithIndex None hub index partition

    let read (wait:TimeSpan) (hub:Hub) =
        Partition.toString
        >> Receiver.toSeq (Some wait) hub None

    let readFrom (wait:TimeSpan) (hub:Hub) =
        Partition.toString >> fun partition ->
            Some >> fun index ->
                Receiver.toSeqWithIndex (Some wait) hub index partition

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