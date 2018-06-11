namespace Nata.IO.KafkaNet

open System
open System.Collections.Generic
open System.Net
open System.Text
open KafkaNet
open KafkaNet.Model

open Nata.Core
open Nata.IO

type TopicPartition =
    { Topic : Topic
      Partition : int }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TopicPartition =

    let internal create (cluster:Cluster) (name:TopicName) (partitionId:int) =
        { TopicPartition.Topic = Topic.create cluster name
          TopicPartition.Partition = partitionId }

    let private offsetRangeFor (consumer:Consumer, topic:TopicName, partition:int) : OffsetRange =
        consumer.GetTopicOffsetAsync(topic,1048576,-1)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Seq.map OffsetRange.fromKafka
        |> Seq.filter (OffsetRange.partitionId >> (=) partition)
        |> Seq.head

    let rec private indexOf (range:OffsetRange) = function
        | Position.Start -> Offset.start range
        | Position.End -> Offset.finish range
        | Position.At x -> x
        | Position.Before x -> (indexOf range x) - 1L
        | Position.After x -> (indexOf range x) + 1L

    let private produce {Topic={Producer=producer;Name=name};Partition=partition} =
        let producer = producer()
        let encode = Message.withPartitionId partition >> Message.toKafka
        fun messages ->
            async {
                return!
                    producer.SendMessageAsync(name, Seq.map encode messages)
                    |> Async.AwaitTask
            }
            |> Async.RunSynchronously
            |> Seq.map Offset.fromKafka

    let private consume ({Topic={Consumer=consumer;Name=name};Partition=partition})
                        (hasCompleted:(OffsetRange*Offset)->bool)
                        (position:Position<Offset>) =
        let offset, range =
            use consumer = consumer [partition]
            let range = offsetRangeFor(consumer, name, partition)
            let offset = indexOf range position
            { offset with Position=Math.Max(0L, offset.Position) }, range
        seq {
            use consumer = consumer [partition]
            use enumerator = 
                consumer.SetOffsetPosition(Offset.toKafka(offset))
                consumer.Consume().GetEnumerator()

            let get(partitions, offset) =
                let result, message =
                    enumerator.MoveNext(),
                    enumerator.Current |> Message.fromKafka
                let offset =
                    offset |> Offset.updateWith message
                (message, offset), (partitions, offset)

            yield! 
                Seq.unfold (fun (partitions, offset) ->
                    (partitions, offset)
                    |> Option.whenTrue (hasCompleted >> not)
                    |> Option.defaultWith (fun _ -> offsetRangeFor(consumer,name,partition), offset)
                    |> Option.whenTrue (hasCompleted >> not)
                    |> Option.map get) (range, offset)
        }

    let index {Topic={Consumer=consumer;Name=name};Partition=partition}  position =
        use consumer = consumer [partition]
        indexOf (offsetRangeFor(consumer, name, partition)) position

    let readFrom topicPartition position =
        consume topicPartition Offset.completed position

    let readFromStart topicPartition =
        consume topicPartition Offset.completed Position.Start

    let read =
        readFromStart >> Seq.map fst

    let listenFrom topicPartition position =
        consume topicPartition Offsets.neverCompleted position

    let listenFromStart topicPartition =
        consume topicPartition Offsets.neverCompleted Position.Start

    let listen =
        listenFromStart >> Seq.map fst

    let write topic =
        Seq.singleton >> produce topic >> ignore
        
    let connect : Connector<Cluster,TopicName*int,Data,Offset> =
        fun cluster (name,partition) ->
            [
                Capability.Indexer <|
                    (index (create cluster name partition))

                Capability.Reader <| fun () ->
                    (read (create cluster name partition) |> Seq.map (Event.ofMessage name))

                Capability.ReaderFrom <|
                    (readFrom (create cluster name partition) >> Seq.mapFst (Event.ofMessage name))

                Capability.Writer <|
                    (Event.toMessage >> write (create cluster name partition))

                Capability.Subscriber <| fun () ->
                    (listen (create cluster name partition) |> Seq.map (Event.ofMessage name))

                Capability.SubscriberFrom <|
                    (listenFrom (create cluster name partition) >> Seq.mapFst (Event.ofMessage name))
            ]