namespace Nata.IO

open System
open FSharp.Data

type Event<'Data,'Metadata> = {
    Type : string
    Stream : string
    Date : DateTime
    Data : 'Data
    Metadata : 'Metadata
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Event =

    let ``type`` (e:Event<_,_>) = e.Type
    let stream (e:Event<_,_>) = e.Stream
    let date (e:Event<_,_>) = e.Date
    let data (e:Event<_,_>) = e.Data
    let metadata (e:Event<_,_>) = e.Metadata

    let mapData (fn:'DataIn->'DataOut)
                (e:Event<'DataIn,'Metadata>) : Event<'DataOut,'Metadata> =
        { Type = e.Type
          Stream = e.Stream
          Date = e.Date
          Data = fn e.Data
          Metadata = e.Metadata }

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (e:Event<'Data,'MetadataIn>) : Event<'Data,'MetadataOut> =
        { Type = e.Type
          Stream = e.Stream
          Date = e.Date
          Data = e.Data
          Metadata = fn e.Metadata }

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (e:Event<'DataIn,'MetadataIn>) : Event<'DataOut,'MetadataOut> =
        { Type = e.Type
          Stream = e.Stream
          Date = e.Date
          Data = dataFn e.Data
          Metadata = metadataFn e.Metadata }
          
    let toJsonValue (event:Event<JsonValue,JsonValue>) =
        JsonValue.Record [|
            "type",     JsonValue.String event.Type
            "stream",   JsonValue.String event.Stream
            "date",     DateTime.toJsonValue event.Date
            "data",     event.Data
            "metadata", event.Metadata 
        |]
        
    let ofJsonValue (json:JsonValue) =
        { Event.Type =     json.["type"].AsString()
          Event.Stream =   json.["stream"].AsString()
          Event.Date =     json.["date"] |> DateTime.ofJsonValue
          Event.Data =     json.["data"]
          Event.Metadata = json.["metadata"] }

    module Codec =
            
        let EventToJson : Codec<Event<JsonValue,JsonValue>,JsonValue> = toJsonValue, ofJsonValue
        let JsonToEvent : Codec<JsonValue,Event<JsonValue,JsonValue>> = ofJsonValue, toJsonValue

        let EventToString = EventToJson |> Codec.concatenate JsonValue.Codec.JsonValueToString
        let StringToEvent = EventToString |> Codec.reverse