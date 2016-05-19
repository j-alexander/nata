namespace Nata.IO.Kafka

open System
open System.Collections.Generic
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

open Nata.IO

type TopicName = string
type Topic =
    { Consumer : unit -> Consumer
      Producer : unit -> Producer
      Name : TopicName }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Topic =

    let private offsetRangesFor (consumer:Consumer, topic:TopicName) : OffsetRanges =
        consumer.GetTopicOffsetAsync(topic)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map OffsetRange.fromKafka
        |> Seq.sortBy OffsetRange.partitionId
        |> Seq.toList

    let private produce (topic:Topic) (messages) =
        topic.Producer().SendMessageAsync(topic.Name, Seq.map Message.toKafka messages)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Offset.fromKafka

    let private consume (topic:Topic) hasCompleted (position:Position<Offsets>) =
        seq {   
            use consumer = topic.Consumer()
            let ranges = offsetRangesFor(consumer, topic.Name)
            let rec start = function
                | Position.Start -> Offsets.start ranges
                | Position.End -> Offsets.finish ranges
                | Position.At x -> x
                | Position.Before x -> Offsets.before ranges (start x)
                | Position.After x -> Offsets.after ranges (start x)
            let offsets = start position

            use enumerator = 
                consumer.SetOffsetPosition(Offsets.toKafka(offsets))
                consumer.Consume().GetEnumerator()

            let get(partitions, offsets) =
                let result, message =
                    enumerator.MoveNext(),
                    enumerator.Current |> Message.fromKafka
                let offsets =
                    offsets |> Offsets.updateWith message
                (message, offsets), (partitions, offsets)

            yield! 
                Seq.unfold (fun (partitions, offsets) ->
                    (partitions, offsets)
                    |> Option.whenTrue (hasCompleted >> not)
                    |> Option.getValueOrYield (fun _ -> offsetRangesFor(consumer,topic.Name), offsets)
                    |> Option.whenTrue (hasCompleted >> not)
                    |> Option.map get) (ranges, offsets)
        }

    let readFrom topic position =
        consume topic Offsets.completed position

    let readFromStart topic =
        consume topic Offsets.completed Position.Start

    let read =
        readFromStart >> Seq.map fst

    let listenFrom topic position =
        consume topic Offsets.neverCompleted position

    let listenFromStart topic =
        consume topic Offsets.neverCompleted Position.Start

    let listen =
        listenFromStart >> Seq.map fst

    let write topic =
        Seq.singleton >> produce topic >> ignore