namespace Nata.IO.Kafka

open System
open System.Collections.Generic
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

        let rec iterate(partitions, offsets) =
            if Offsets.completed (partitions, offsets) then

                (partitionsFor topic, offsets)
                |> function | x when Offsets.completed x -> Seq.empty
                            | x -> iterate x
            else
                seq {
                    let result = enumerator.MoveNext()
                    let message = enumerator.Current |> Message.fromMessage 
                    let offsets = offsets |> Offsets.updateWith message

                    yield message, offsets
                    yield! iterate(partitions, offsets)
                }

        iterate((partitionsFor topic), offsets)

    let readFromStart topic =
        readFrom topic (Offsets.start (partitionsFor topic))