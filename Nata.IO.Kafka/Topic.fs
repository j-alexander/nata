namespace Nata.IO.Kafka

open System
open System.Collections.Generic
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

open Nata.IO

type Topic =
    { Consumer : Consumer
      Producer : Producer
      Name : string }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Topic =

    let partitionsFor (topic:Topic) : Partitions =
        topic.Consumer.GetTopicOffsetAsync(topic.Name)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Partition.fromKafka
        |> Seq.sortBy Partition.id
        |> Seq.toList

    let private produce (topic:Topic) (messages) =
        topic.Producer.SendMessageAsync(topic.Name, Seq.map Message.toKafka messages)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Offset.fromKafka

    let private consume topic hasCompleted offsets =

        let partitions = partitionsFor topic
        let start _ = Offsets.start partitions
        let offsets = Option.bindNone start offsets

        let enumerator = 
            topic.Consumer.SetOffsetPosition(Offsets.toKafka(offsets))
            topic.Consumer.Consume().GetEnumerator()

        let get(partitions, offsets) =
            let result, message =
                enumerator.MoveNext(),
                enumerator.Current |> Message.fromKafka
            let offsets =
                offsets |> Offsets.updateWith message
            (message, offsets), (partitions, offsets)

        Seq.unfold (fun (partitions, offsets) ->
            (partitions, offsets)
            |> Option.whenTrue (hasCompleted >> not)
            |> Option.bindNone (fun _ -> partitionsFor topic, offsets)
            |> Option.whenTrue (hasCompleted >> not)
            |> Option.map get) (partitions, offsets)

    let readFrom topic offsets =
        consume topic Offsets.completed (Some offsets)

    let readFromStart topic =
        consume topic Offsets.completed None

    let read =
        readFromStart >> Seq.map fst

    let listenFrom topic offsets =
        consume topic Offsets.neverCompleted (Some offsets)

    let listenFromStart topic =
        consume topic Offsets.neverCompleted None

    let listen =
        listenFromStart >> Seq.map fst

    let write topic =
        Seq.singleton >> produce topic >> ignore