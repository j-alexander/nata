namespace Nata.IO.KafkaNet

open System
open System.Net
open System.Text
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

    let withPartitionId id (x:Message) = { x with PartitionId = id }
    let withOffset offset (x:Message) =  { x with Offset = offset }
    let withKey key (x:Message) =        { x with Key = key }
    let withValue value (x:Message) =    { x with Value = value }

    let fromKafka (x:KafkaNet.Protocol.Message) =
        { Message.PartitionId = x.Meta.PartitionId
          Message.Offset = x.Meta.Offset
          Message.Key = x.Key
          Message.Value = x.Value }

    let toKafka (x:Message) =
        let message = new KafkaNet.Protocol.Message()
        message.Key <- x.Key
        message.Value <- x.Value
        message