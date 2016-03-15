namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
    
type Message =
    { PartitionId : int
      Offset : int64 
      Key : byte[]
      Value : byte[] }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Message =
    
    let partitionId (x:Message) = x.PartitionId
    let offset (x:Message) = x.Offset
    let key (x:Message) = x.Key
    let value (x:Message) = x.Value

    let fromMessage (x:KafkaNet.Protocol.Message) =
        { Message.PartitionId = x.Meta.PartitionId
          Message.Offset = x.Meta.Offset
          Message.Key = x.Key
          Message.Value = x.Value }