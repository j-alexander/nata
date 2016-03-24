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
        |> Event.Source.withKey (message.Key |> Encoding.Default.GetString)
        |> Event.Source.withStream topic
        |> Event.Source.withPartition message.PartitionId
        |> Event.Source.withIndex message.Offset

    let toMessage (partitionId:int) (offset:int64) (event:Event) =
        { Message.Value = event.Data
          Message.PartitionId =
            Event.Target.partition event
            |> Option.coalesce (Event.Source.partition event)
            |> Option.bindNone (fun () -> 0)
          Message.Offset =
            Event.Target.index event
            |> Option.coalesce (Event.Source.index event)
            |> Option.bindNone (fun () -> 0L)
          Message.Key =
            Event.Target.key event
            |> Option.filter (String.IsNullOrWhiteSpace >> not)
            |> Option.coalesce (Event.Source.key event)
            |> Option.filter (String.IsNullOrWhiteSpace >> not)
            |> Option.map (Encoding.Default.GetBytes)
            |> Option.bindNone (fun () -> Guid.NewGuid().ToByteArray()) }