namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Hub = EventHubClient

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Hub =

    let create (settings:Settings) : Hub =
        EventHubClient.CreateFromConnectionString(settings.Connection)

    let partitions (hub:Hub) : Partition[] =
        hub.GetRuntimeInformation().PartitionIds
        |> Array.map Int32.Parse

    let write (hub:Hub) (event:Event<byte[]>) =
        let data = new EventData(event.Data)
        data.PartitionKey <- 
            event
            |> Event.key
            |> Option.getValueOr (guid())
        data
        |> hub.Send

    let subscribe (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        hub.GetRuntimeInformation().PartitionIds
        |> Seq.map (group.CreateReceiver >> Receiver.toSeq None)
        |> Seq.toList
        |> Seq.merge

    let connect : Connector<Settings,unit,byte[],unit> =

        fun settings ->
        
            let hub = settings |> create

            fun _ ->

                [
                    Nata.IO.Writer <|
                        write hub

                    Nata.IO.Subscriber <| fun () ->
                        subscribe hub
                ]       