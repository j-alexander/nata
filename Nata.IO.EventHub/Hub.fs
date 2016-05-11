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

    let partitions (client:Hub) : Partition[] =
        client.GetRuntimeInformation().PartitionIds

    let write (client:Hub) (event:Event<byte[]>) =
        let data = new EventData(event.Data)
        data.PartitionKey <- 
            event
            |> Event.partition
            |> Option.map (fun x -> x.ToString())
            |> Option.getValueOr (guid())
        data
        |> client.Send