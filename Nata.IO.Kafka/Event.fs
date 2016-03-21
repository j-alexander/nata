namespace Nata.IO.Kafka

open System
open Nata.IO

type Data = byte[]
type Metadata = byte[]
type Event = Event<Data, Metadata> 

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Event =

    let ofMessage (topic:TopicName) (message:Message) =
        { Event.Date = DateTime.UtcNow
          Event.Type = topic
          Event.Stream = topic
          Event.Data = message.Value
          Event.Metadata = message.Key }

    let toMessage (partitionId:int) (offset:int64) (event:Event) =
        { Message.PartitionId = 0
          Message.Offset = 0L
          Message.Key =
            match event.Metadata with
            | [||] -> Guid.NewGuid().ToByteArray()
            | meta -> meta
          Message.Value = event.Data}
