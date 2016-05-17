namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Hub = EventHubClient
type Partition = string

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Hub =

    let create (settings:Settings) : Hub =
        EventHubClient.CreateFromConnectionString(settings.Connection)

    let partitions (hub:Hub) : Partition[] =
        hub.GetRuntimeInformation().PartitionIds

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
        hub
        |> partitions
        |> Array.map (group.CreateReceiver >> Receiver.toSeq)
        |> Array.toList
        |> Seq.merge

    let connect : Connector<Hub,unit,byte[],unit> =
        fun hub _ ->
            [
                Nata.IO.Writer <|
                    write hub

                Nata.IO.Subscriber <| fun () ->
                    subscribe hub
            ]       