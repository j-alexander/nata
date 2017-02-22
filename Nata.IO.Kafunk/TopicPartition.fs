namespace Nata.IO.Kafka

open System
open System.Text

open Nata.Core
open Nata.IO
open Kafunk

type TopicPartition =
    { Topic : Topic
      Partition : Partition }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TopicPartition =

    let fetch {Cluster=cluster;Settings=settings} topic partition offset =

        let request =
            let replicaId : ReplicaId = -1
            new FetchRequest(
                replicaId,
                settings.FetchMaxWaitTime.TotalMilliseconds |> int,
                settings.FetchMinBytes,
                [|
                    topic,
                    [|
                        partition,
                        offset,
                        settings.FetchMaxBytes
                    |]
                |])

        let response =
            Kafka.fetch cluster request
            |> Async.RunSynchronously

        match response.topics with
        | [| t, [| p, ec, hwmo, mss, ms |] |] ->
            match ec with
            | ErrorCode.NoError ->          Some(ConsumerMessageSet(t, p, ms, hwmo))
            | ErrorCode.OffsetOutOfRange -> None
            | x ->
                x
                |> Error.check
                |> raise
        | x ->
            x
            |> sprintf "Unrecognized fetch response: %A" 
            |> failwith

    let consume live connection topic partition =
        Seq.unfold(fun offset ->
            match fetch connection topic partition offset with
            | None -> None
            | Some cms ->
                match cms.messageSet.messages with
                | [||] when not live -> None
                | [||] ->
                    Some(None, offset)
                | messages ->
                    let offset =
                        messages
                        |> Seq.map (fun (o,s,m) -> o)
                        |> Seq.max
                        |> (+) 1L
                    Some(Some cms,offset))
        >> Seq.choose id
        >> Seq.collect (fun cms ->
            cms.messageSet.messages
            |> Seq.map (fun (offset,size,message) ->
                let key = Binary.toArray message.key
                let value = Binary.toArray message.value
                let timestamp = 
                    match message.timestamp with
                    | 0L -> None
                    | ts -> Some(DateTime.ofUnixMilliseconds ts)

                Event.create value
                |> Event.withKey (Convert.ToBase64String key)
                |> Event.withPartition cms.partition
                |> Event.withStream cms.topic
                |> Event.withIndex offset
                |> Event.withSentAtOption timestamp,
                { Offset.PartitionId=partition
                  Offset.Position=offset }))

    let rec private indexOf (range:OffsetRange) = function
        | Position.Start -> Offset.start range
        | Position.End -> Offset.finish range
        | Position.At x -> x
        | Position.Before x -> (indexOf range x) - 1L
        | Position.After x -> (indexOf range x) + 1L

    let consumeFrom live connection topic partition position =
        match OffsetRange.query connection topic partition with
        | None -> Seq.empty
        | Some range ->
            indexOf range position
            |> Offset.position
            |> consume live connection topic partition

    let index connection topic partition position =
        match OffsetRange.query connection topic partition with
        | Some range -> indexOf range position
        | None ->
            sprintf "Topic '%s' w/ Partition %d Not Found" topic partition
            |> failwith

    let write { Cluster=cluster; Settings=settings } topic partition =
        let configuration =
            let partitioner = Partitioner.konst partition
            ProducerConfig.create(topic, partitioner)
        let producer =
            Producer.create cluster configuration
        fun (event:Event<Data>) ->
            let bytes =
                event
                |> Event.data,
                event
                |> Event.key
                |> Option.map Encoding.UTF8.GetBytes
                |> Option.getValueOrYield guidBytes
            bytes
            |> ProducerMessage.ofBytes
            |> Producer.produce producer
            |> Async.RunSynchronously
            |> fun (result:ProducerResult) ->
                if partition <> result.partition then
                    sprintf "Commit partition mismatch %d != %d" partition result.partition
                    |> failwith

    let connect : Connector<_,_,_,_> =
        fun (connection) (topic,partition) ->
            [
                Capability.Indexer <|
                    index connection topic partition

                Capability.Reader <| fun () ->
                    consumeFrom false connection topic partition Position.Start
                    |> Seq.map fst

                Capability.ReaderFrom <|
                    consumeFrom false connection topic partition

                Capability.Writer <|
                    write connection topic partition

                Capability.Subscriber <| fun () ->
                    consumeFrom true connection topic partition Position.Start
                    |> Seq.map fst

                Capability.SubscriberFrom <|
                    consumeFrom true connection topic partition
            ]
            

