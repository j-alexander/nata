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
      Name : string }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Topic =

    let connect (cluster:Cluster) (name:string) : Topic =
        { Consumer = new Consumer(new ConsumerOptions(name, cluster))
          Name = name }

    let partitionsFor (topic:Topic) : Partitions =
        topic.Consumer.GetTopicOffsetAsync(topic.Name)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Partition.fromOffsetResponse
        |> Seq.sortBy Partition.id
        |> Seq.toList

    let private iterate (hasCompleted, topic, offsets) =

        let partitions = partitionsFor topic
        let start _ = Offsets.start partitions
        let offsets = Option.bindNone start offsets

        let enumerator = 
            topic.Consumer.SetOffsetPosition(Offsets.toOffsetPosition(offsets))
            topic.Consumer.Consume().GetEnumerator()

        (partitions, offsets)
        |> Seq.unfold (fun (partitions, offsets) ->
            (partitions, offsets)
            |> Option.whenTrue (hasCompleted >> not)
            |> Option.bindNone (fun _ -> partitionsFor topic, offsets)
            |> Option.whenTrue (hasCompleted >> not)
            |> Option.map(fun (partitions, offsets) ->
                let result, message =
                    enumerator.MoveNext(),
                    enumerator.Current |> Message.fromMessage
                let offsets = offsets |> Offsets.updateWith message
                (message, offsets),(partitions, offsets)))

    let readFrom topic offsets =
        iterate(Offsets.completed, topic, Some offsets)

    let readFromStart topic =
        iterate(Offsets.completed, topic, None)

    let listenFrom topic offsets =
        iterate((fun _ -> false), topic, Some offsets)

    let listenFromStart topic =
        iterate((fun _ -> false), topic, None)