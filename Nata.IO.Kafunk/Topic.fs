namespace Nata.IO.Kafka

open System
open System.Text

open Nata.Core
open Nata.IO
open Kafunk

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Topic =

    
    let rec private indexOf (ranges:OffsetRanges) = function
        | Position.Start -> Offsets.start ranges
        | Position.End -> Offsets.finish ranges
        | Position.At x -> x
        | Position.Before x -> Offsets.before ranges (indexOf ranges x)
        | Position.After x -> Offsets.after ranges (indexOf ranges x)

    let consumeFrom live (connection,settings) topic position =
        match OffsetRange.queryAll connection topic with
        | [] -> Seq.empty
        | ranges ->
            seq {
                let (Offsets offsets) = indexOf ranges position
                let events =
                    Seq.merge [
                        for { Offset.PartitionId=partition
                              Offset.Position=position } as offset in offsets ->
                              Position.At offset
                              |> TopicPartition.consumeFrom live (connection,settings) topic partition
                    ]
                use enumerator = events.GetEnumerator()
                let rec loop (offsets) =
                    seq {
                        if enumerator.MoveNext() then
                            let event, offset = enumerator.Current
                            let offsets =
                                [
                                    for { Offset.PartitionId=p } as o in offsets ->
                                        if offset.PartitionId = p then offset
                                        else o
                                ]
                            yield event, Offsets offsets
                            yield! loop offsets
                    }
                yield! loop offsets
            }

    let index connection topic position =
        match OffsetRange.queryAll connection topic with
        | [] ->
            sprintf "Topic '%s' Not Found" topic
            |> failwith
        | offsets -> indexOf offsets position

    let write (connection,settings) topic (event:Event<Data>) =
        let partitioner =
            event
            |> Event.partition
            |> Option.map Partitioner.konst
            |> Option.getValueOr Partitioner.crc32Key
        let producer =
            ProducerConfig.create(topic, partitioner)
            |> Producer.create connection
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
        |> fun (result:ProducerResult) -> ()
                

    let connect : Connector<_,_,_,_> =
        Settings.connect >> fun (connection,settings) (topic) ->
            [
                Capability.Indexer <|
                    index connection topic

                Capability.Reader <| fun () ->
                    consumeFrom false (connection,settings) topic Position.Start
                    |> Seq.map fst

                Capability.ReaderFrom <|
                    consumeFrom false (connection,settings) topic

                Capability.Writer <|
                    write (connection,settings) topic

                Capability.Subscriber <| fun () ->
                    consumeFrom true (connection,settings) topic Position.Start
                    |> Seq.map fst

                Capability.SubscriberFrom <|
                    consumeFrom true (connection,settings) topic
            ]
