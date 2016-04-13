namespace Nata.IO

open System
open FSharp.Data

type Event<'Data> = {
    Data : 'Data
    Metadata : Value list
    At : DateTime
}
and Value =
    | CreatedAt of DateTime
    | SentAt of DateTime
    | ReceivedAt of DateTime
    | EventType of string
    | Name of string
    | Stream of string
    | Partition of int
    | Key of string
    | Tag of string
    | Index of int64
    | Bytes of byte[]

    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Value =

    let createdAt =
        function Value.CreatedAt  x -> Some x | _ -> None
    let sentAt =
        function Value.SentAt     x -> Some x | _ -> None
    let receivedAt =
        function Value.ReceivedAt x -> Some x | _ -> None
    let eventType =
        function Value.EventType  x -> Some x | _ -> None
    let name =
        function Value.Name       x -> Some x | _ -> None
    let stream =
        function Value.Stream     x -> Some x | _ -> None
    let tag =
        function Value.Tag        x -> Some x | _ -> None
    let key =
        function Value.Key        x -> Some x | _ -> None
    let partition =
        function Value.Partition  x -> Some x | _ -> None
    let index =
        function Value.Index      x -> Some x | _ -> None
    let bytes =
        function Value.Bytes      x -> Some x | _ -> None

    let toJsonValue =
        function
        | Value.CreatedAt x ->  "createdAt",  DateTime.toJsonValue x
        | Value.SentAt x ->     "sentAt",     DateTime.toJsonValue x
        | Value.ReceivedAt x -> "receivedAt", DateTime.toJsonValue x
        | Value.EventType x ->  "eventType",  JsonValue.String x
        | Value.Name x ->       "name",       JsonValue.String x
        | Value.Stream x ->     "stream",     JsonValue.String x
        | Value.Tag x ->        "tag",        JsonValue.String x
        | Value.Key x ->        "key",        JsonValue.String x
        | Value.Partition x ->  "partition",  JsonValue.Number (decimal x)
        | Value.Index x ->      "index",      JsonValue.Number (decimal x)
        | Value.Bytes x ->      "bytes",      JsonValue.Null

    let ofJsonValue = function
        | "createdAt", json ->  json |> DateTime.ofJsonValue |> Value.CreatedAt  |> Some
        | "sentAt", json ->     json |> DateTime.ofJsonValue |> Value.SentAt     |> Some
        | "receivedAt", json -> json |> DateTime.ofJsonValue |> Value.ReceivedAt |> Some
        | "eventType", json ->  json.AsString()              |> Value.EventType  |> Some
        | "name", json ->       json.AsString()              |> Value.Name       |> Some
        | "stream", json ->     json.AsString()              |> Value.Stream     |> Some
        | "tag", json ->        json.AsString()              |> Value.Tag        |> Some
        | "key", json ->        json.AsString()              |> Value.Key        |> Some
        | "partition", json ->  json.AsInteger()             |> Value.Partition  |> Some
        | "index", json ->      json.AsInteger64()           |> Value.Index      |> Some
        | "bytes", json ->      [||]                         |> Value.Bytes      |> Some
        | _ ->                  None

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Event =

    let data (e:Event<_>) = e.Data
    let metadata (e:Event<_>) = e.Metadata
    let at (e:Event<_>) = e.At

    let valueOf fn = metadata >> List.tryPick fn
    
    let createdAt e =  e |> valueOf Value.createdAt
    let sentAt e =     e |> valueOf Value.sentAt
    let receivedAt e = e |> valueOf Value.receivedAt
    let eventType e =  e |> valueOf Value.eventType
    let name e =       e |> valueOf Value.name
    let stream e =     e |> valueOf Value.stream
    let tag e =        e |> valueOf Value.tag
    let key e =        e |> valueOf Value.key
    let partition e =  e |> valueOf Value.partition
    let index e =      e |> valueOf Value.index
    let bytes e =      e |> valueOf Value.bytes

    let createAt time data =
        { Event.Data = data
          Event.Metadata = []
          Event.At = time }

    let create data =
        createAt DateTime.UtcNow data
    
    let withMetadata x e =
        { e with Metadata = x :: e.Metadata }

    let withCreatedAt x =  withMetadata (x |> Value.CreatedAt)
    let withSentAt x =     withMetadata (x |> Value.SentAt)
    let withReceivedAt x = withMetadata (x |> Value.ReceivedAt)
    let withEventType x =  withMetadata (x |> Value.EventType)
    let withName x =       withMetadata (x |> Value.Name)
    let withStream x =     withMetadata (x |> Value.Stream)
    let withTag x =        withMetadata (x |> Value.Tag)
    let withKey x =        withMetadata (x |> Value.Key)
    let withPartition x =  withMetadata (x |> Value.Partition)
    let withIndex x =      withMetadata (x |> Value.Index)
    let withBytes x =      withMetadata (x |> Value.Bytes)

    let mapData (fn:'DataIn->'DataOut)
                (e:Event<'DataIn>) : Event<'DataOut> =
        { Data = fn e.Data
          Metadata = e.Metadata
          At = e.At }

    let map : ('DataIn->'DataOut) -> Event<'DataIn> -> Event<'DataOut> = mapData
          
    let toJsonValue (event:Event<JsonValue>) =
        JsonValue.Record [|
            "data", event.Data
            "metadata",
                event.Metadata
                |> Seq.map Value.toJsonValue
                |> Seq.toArray
                |> JsonValue.Record
            "at",
                event.At
                |> DateTime.toJsonValue 
        |]
        
    let ofJsonValue (json:JsonValue) =
        { Event.Data = json.["data"]
          Event.Metadata =
            json.["metadata"].Properties()
            |> Seq.choose Value.ofJsonValue
            |> Seq.toList
          Event.At =
            json.["at"]
            |> DateTime.ofJsonValue }

    module Codec =
            
        let EventToJson : Codec<Event<JsonValue>,JsonValue> = toJsonValue, ofJsonValue
        let JsonToEvent : Codec<JsonValue,Event<JsonValue>> = ofJsonValue, toJsonValue

        let EventToString = EventToJson |> Codec.concatenate JsonValue.Codec.JsonValueToString
        let StringToEvent = EventToString |> Codec.reverse