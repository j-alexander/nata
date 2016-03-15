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

    let partitions (topic:Topic) : Partitions =
        topic.Consumer.GetTopicOffsetAsync(topic.Name)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map Partition.fromOffsetResponse
        |> Seq.sortBy Partition.id
        |> Seq.toList


    let readFrom (topic:Topic) (offsets:Offsets) =

        let initial_partitions = partitions topic
        let offsets = Offsets.startFrom(initial_partitions, offsets)
        let start = Offsets.toOffsetPosition(offsets)

        let stream = 
            topic.Consumer.SetOffsetPosition(start)
            topic.Consumer.Consume().GetEnumerator()

        let rec iterate current_partitions offsets =
            if Offsets.completed (current_partitions, offsets) then
                let updated_partitions = partitions topic
                if Offsets.completed (updated_partitions, offsets) then
                    Seq.empty
                else
                    iterate updated_partitions offsets
            else
                seq {
                    let result = stream.MoveNext()
                    let message = stream.Current |> Message.fromMessage 
                    let offsets = offsets |> Offsets.updateWith message

                    yield message, offsets
                    yield! iterate current_partitions offsets
                }

        iterate initial_partitions offsets

    let readFromStart (topic:Topic) =
        partitions topic
        |> Offsets.start
        |> readFrom topic