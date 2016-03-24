namespace Nata.IO.Kafka

open System
open System.Text
open Nata.IO

type Data = byte[]
type Event = Event<Data> 

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Event =

    let ofMessage (topic:TopicName) (message:Message) =
        Event.create message.Value
        |> Event.withKey (message.Key |> Encoding.Default.GetString)
        |> Event.withStream topic
        |> Event.withPartition message.PartitionId
        |> Event.withIndex message.Offset

    let toMessage (partitionId:int) (offset:int64) (event:Event) =
        { Message.Value = event.Data
          Message.PartitionId =
            match Event.partition event with
            | Some x -> x
            | None -> 0
          Message.Offset =
            match Event.index event with
            | Some x -> x
            | None -> 0L
          Message.Key =
            match Event.key event with
            | Some x when String.IsNullOrEmpty x ->
                Guid.NewGuid().ToByteArray()
            | None ->
                Guid.NewGuid().ToByteArray()
            | Some key ->
                Encoding.Default.GetBytes key }
