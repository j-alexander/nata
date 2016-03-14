namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model

type Topic =
    { Consumer : Consumer
      Name : string }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Topic =

    let connect (cluster:Cluster) (name:string) : Topic =
        { Consumer = new Consumer(new ConsumerOptions(name, cluster))
          Name = name }

    let offsets (topic:Topic) : Offset list =
        topic.Consumer.GetTopicOffsetAsync(topic.Name)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Offset.fromResponse
        |> Seq.sortBy Offset.partitionId
        |> Seq.toList