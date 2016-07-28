namespace Nata.IO.EventHub

open System
open Microsoft.ServiceBus.Messaging
open Nata.IO

type Hub = EventHubClient

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Hub =


    let positionOf (hub:Hub) (partition:Partition) : Position<Index> -> Index =
        
        let index_finish, index_start =
            let information =
                partition
                |> Partition.toString
                |> hub.GetPartitionRuntimeInformation
            information.LastEnqueuedOffset
            |> Index.ofString
            |> Option.getValueOr Index.start, Index.start

        let rec indexOf = function
            | Position.Start -> index_start
            | Position.Before x -> indexOf x - 1L
            | Position.At x -> x
            | Position.After x -> indexOf x + 1L
            | Position.End -> index_finish

        indexOf >> Index.between(index_start, index_finish)


    let positionsOf (hub:Hub) : Position<Offsets> -> Offsets =

        let positionOf = positionOf hub |> swap
        let partitions =
            hub.GetRuntimeInformation().PartitionIds
            |> Seq.choose Partition.ofString
            |> Seq.toList
        
        let rec indexOf = function
            | Position.Start ->
                [ for partition in partitions ->
                    { Offset.Partition = partition
                      Offset.Index = positionOf Position.Start partition } ]
            | Position.Before x ->
                [ for offset in indexOf x ->
                    { offset with Index = offset.Index - 1L } ]
            | Position.At x -> x
            | Position.After x ->
                [ for offset in indexOf x ->
                    { offset with Index = offset.Index + 1L } ]
            | Position.End ->
                [ for partition in partitions ->
                    { Offset.Partition = partition
                      Offset.Index = positionOf Position.End partition } ]

        indexOf
        

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
        |> Seq.map (Receiver.toSeq None group None)
        |> Seq.toList
        |> Seq.merge

    let subscribeFrom (hub:Hub) (offsets:Offsets) =
        let group = hub.GetDefaultConsumerGroup()
        hub.GetRuntimeInformation().PartitionIds
        |> Seq.map(fun partition ->
            let start =
                offsets
                |> List.tryFind (Offset.partition >> Partition.toString >> (=) partition)
                |> Option.map (Offset.index >> Index.toString)
            Receiver.toSeqWithOffset None group start partition)
        |> Seq.toList
        |> Seq.merge
        |> Offsets.merge offsets

    let read (wait:TimeSpan) (hub:Hub) =
        let group = hub.GetDefaultConsumerGroup()
        hub.GetRuntimeInformation().PartitionIds
        |> Seq.map (Receiver.toSeq (Some wait) group None)
        |> Seq.toList
        |> Seq.merge
        
    let subscribeFromPosition (hub:Hub) =
        positionsOf hub >> subscribeFrom hub

    let connect : Connector<Settings,unit,byte[],Offsets> =

        fun settings ->
        
            let hub, wait =
                settings |> create,
                settings.MaximumWaitTimeOnRead

            fun _ ->
                [
                    Nata.IO.Reader <| fun () ->
                        read wait hub

                    Nata.IO.Writer <|
                        write hub

                    Nata.IO.Subscriber <| fun () ->
                        subscribe hub

                    Nata.IO.SubscriberFrom <|
                        subscribeFromPosition hub
                ]       