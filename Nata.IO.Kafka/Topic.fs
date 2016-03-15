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

    let readFrom (topic:Topic) (offsets:Offsets) =

        let enumerator = 
            topic.Consumer.SetOffsetPosition(Offsets.toOffsetPosition(offsets))
            topic.Consumer.Consume().GetEnumerator()

        ((partitionsFor topic), offsets)
        |> Seq.unfold (fun (partitions, offsets) ->
            (partitions, offsets)
            |> Option.whenTrue (Offsets.completed >> not)
            |> Option.bindNone (fun _ -> partitionsFor topic, offsets)
            |> Option.whenTrue (Offsets.completed >> not)
            |> Option.map(fun (partitions, offsets) ->
                let result, message =
                    enumerator.MoveNext(),
                    enumerator.Current |> Message.fromMessage 
                let offsets = offsets |> Offsets.updateWith message
                (message, offsets),(partitions, offsets)))

    let readFromStart topic =
        readFrom topic (Offsets.start (partitionsFor topic))