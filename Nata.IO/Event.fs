namespace Nata.IO

open System
open FSharp.Data

type Event<'Data> = {
    Data : 'Data
    Source : Metadata option
    Target : Metadata option
    At : DateTime
}
and Metadata = {
    Name : string
    Values : Value list
}
and Value =
    | CreatedAt of DateTime
    | SentAt of DateTime
    | ReceivedAt of DateTime
    | EventType of string
    | Stream of string
    | Partition of int
    | Key of string
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
    let stream =
        function Value.Stream     x -> Some x | _ -> None
    let partition =
        function Value.Partition  x -> Some x | _ -> None
    let key =
        function Value.Key        x -> Some x | _ -> None
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
        | Value.Stream x ->     "stream",     JsonValue.String x
        | Value.Partition x ->  "partition",  JsonValue.Number (decimal x)
        | Value.Key x ->        "key",        JsonValue.Number (decimal x)
        | Value.Index x ->      "index",      JsonValue.Number (decimal x)
        | Value.Bytes x ->      "bytes",      JsonValue.Null

    let ofJsonValue = function
        | "createdAt", json ->  json |> DateTime.ofJsonValue |> Value.CreatedAt  |> Some
        | "sentAt", json ->     json |> DateTime.ofJsonValue |> Value.SentAt     |> Some
        | "receivedAt", json -> json |> DateTime.ofJsonValue |> Value.ReceivedAt |> Some
        | "eventType", json ->  json.AsString()              |> Value.EventType  |> Some
        | "stream", json ->     json.AsString()              |> Value.Stream     |> Some
        | "partition", json ->  json.AsInteger()             |> Value.Partition  |> Some
        | "key", json ->        json.AsString()              |> Value.Key        |> Some
        | "index", json ->      json.AsInteger64()           |> Value.Index      |> Some
        | "bytes", json ->      [||]                         |> Value.Bytes      |> Some
        | _ ->                  None
        
        

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Metadata =

    let name (x:Metadata) = x.Name
    let values (x:Metadata) = x.Values

    let valueOf fn = values >> List.tryPick fn
    
    let createdAt =  valueOf Value.createdAt
    let sentAt =     valueOf Value.sentAt
    let receivedAt = valueOf Value.receivedAt
    let eventType =  valueOf Value.eventType
    let stream =     valueOf Value.stream
    let partition =  valueOf Value.partition
    let key =        valueOf Value.key
    let index =      valueOf Value.index
    let bytes =      valueOf Value.bytes

    let toJsonValue (x:Metadata) =
        JsonValue.Record [|
            "name", x.Name |> JsonValue.String
            "values", x.Values |> Seq.map Value.toJsonValue |> Seq.toArray |> JsonValue.Record
        |]

    let ofJsonValue (json:JsonValue) =
        { Metadata.Name = json.["name"].AsString()
          Metadata.Values =
            json.["values"].Properties()
            |> Seq.choose Value.ofJsonValue
            |> Seq.toList }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Event =

    let data (e:Event<_>) = e.Data
    let source (e:Event<_>) = e.Source
    let target (e:Event<_>) = e.Target
    let at (e:Event<_>) = e.At

    let createAt time data =
        { Event.Data = data
          Event.At = time
          Event.Source = None
          Event.Target = None }
    let create data =
        createAt DateTime.UtcNow data
    
    module Source =

        let name x =       x |> source |> Option.map  Metadata.name
        let createdAt x =  x |> source |> Option.bind Metadata.createdAt
        let sentAt x =     x |> source |> Option.bind Metadata.sentAt
        let receivedAt x = x |> source |> Option.bind Metadata.receivedAt
        let eventType x =  x |> source |> Option.bind Metadata.eventType
        let stream x =     x |> source |> Option.bind Metadata.stream
        let partition x =  x |> source |> Option.bind Metadata.partition
        let key x =        x |> source |> Option.bind Metadata.key
        let index x =      x |> source |> Option.bind Metadata.index
        let bytes x =      x |> source |> Option.bind Metadata.bytes

        let withName x e =
            match source e with
            | Some s -> { e with Source = Some { s with Name = x } }
            | None -> { e with Source = Some { Name = x; Values = [] }}
        let withValue x e =
            match source e with
            | Some s -> { e with Source = Some { s with Values = x :: s.Values }}
            | None -> { e with Source = Some { Name = ""; Values = [x] }}

        let withCreatedAt x =  withValue (x |> Value.CreatedAt)
        let withSentAt x =     withValue (x |> Value.SentAt)
        let withReceivedAt x = withValue (x |> Value.ReceivedAt)
        let withEventType x =  withValue (x |> Value.EventType)
        let withStream x =     withValue (x |> Value.Stream)
        let withPartition x =  withValue (x |> Value.Partition)
        let withKey x =        withValue (x |> Value.Key)
        let withIndex x =      withValue (x |> Value.Index)
        let withBytes x =      withValue (x |> Value.Bytes)
    
    module Target =

        let name x =       x |> target |> Option.map  Metadata.name
        let createdAt x =  x |> target |> Option.bind Metadata.createdAt
        let sentAt x =     x |> target |> Option.bind Metadata.sentAt
        let receivedAt x = x |> target |> Option.bind Metadata.receivedAt
        let eventType x =  x |> target |> Option.bind Metadata.eventType
        let stream x =     x |> target |> Option.bind Metadata.stream
        let partition x =  x |> target |> Option.bind Metadata.partition
        let key x =        x |> target |> Option.bind Metadata.key
        let index x =      x |> target |> Option.bind Metadata.index
        let bytes x =      x |> target |> Option.bind Metadata.bytes

        let withName x e =
            match source e with
            | Some s -> { e with Target = Some { s with Name = x } }
            | None -> { e with Target = Some { Name = x; Values = [] }}
        let withValue x e =
            match source e with
            | Some s -> { e with Target = Some { s with Values = x :: s.Values }}
            | None -> { e with Target = Some { Name = ""; Values = [x] }}

        let withCreatedAt x =  withValue (x |> Value.CreatedAt)
        let withSentAt x =     withValue (x |> Value.SentAt)
        let withReceivedAt x = withValue (x |> Value.ReceivedAt)
        let withEventType x =  withValue (x |> Value.EventType)
        let withStream x =     withValue (x |> Value.Stream)
        let withPartition x =  withValue (x |> Value.Partition)
        let withKey x =        withValue (x |> Value.Key)
        let withIndex x =      withValue (x |> Value.Index)
        let withBytes x =      withValue (x |> Value.Bytes)

    let mapData (fn:'DataIn->'DataOut)
                (e:Event<'DataIn>) : Event<'DataOut> =
        { Data = fn e.Data
          Source = e.Source
          Target = e.Target
          At = e.At }

    let map : ('DataIn->'DataOut) -> Event<'DataIn> -> Event<'DataOut> = mapData
          
    let toJsonValue (event:Event<JsonValue>) =
        JsonValue.Record [|
            yield "data", event.Data
            yield "at", DateTime.toJsonValue event.At
            if event.Source.IsSome then
                yield "source", Metadata.toJsonValue event.Source.Value
            if event.Target.IsSome then
                yield "target", Metadata.toJsonValue event.Target.Value
        |]
        
    let ofJsonValue (json:JsonValue) =
        { Event.Data = json.["data"]
          Event.At = json.["at"] |> DateTime.ofJsonValue
          Event.Source =
            json.TryGetProperty("source")
            |> Option.map(Metadata.ofJsonValue)
          Event.Target = 
            json.TryGetProperty("target")
            |> Option.map(Metadata.ofJsonValue) }

    module Codec =
            
        let EventToJson : Codec<Event<JsonValue>,JsonValue> = toJsonValue, ofJsonValue
        let JsonToEvent : Codec<JsonValue,Event<JsonValue>> = ofJsonValue, toJsonValue

        let EventToString = EventToJson |> Codec.concatenate JsonValue.Codec.JsonValueToString
        let StringToEvent = EventToString |> Codec.reverse