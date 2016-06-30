namespace Nata.IO

open System.Text
open FSharp.Data
open Newtonsoft.Json

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    let toString (json:JsonValue) = json.ToString(JsonSaveOptions.DisableFormatting)
    let toBytes = toString >> fst Codec.StringToBytes
    let toType (json:JsonValue) : 'T = JsonConvert.DeserializeObject<'T>(toString json)

    let ofString (json:string) = JsonValue.Parse json
    let ofBytes = fst Codec.BytesToString >> ofString
    let ofType (t:'T) : JsonValue = JsonConvert.SerializeObject(t) |> ofString

    module Codec =

        let JsonValueToString : Codec<JsonValue,string> = toString, ofString
        let StringToJsonValue : Codec<string,JsonValue> = ofString, toString

        let JsonValueToBytes : Codec<JsonValue,byte[]> = toBytes, ofBytes
        let BytesToJsonValue : Codec<byte[],JsonValue> = ofBytes, toBytes

        let createJsonValueToType() : Codec<JsonValue,'T> = toType, ofType
        let createTypeToJsonValue() : Codec<'T,JsonValue> = ofType, toType

        let createTypeToString() = createTypeToJsonValue() |> Codec.concatenate JsonValueToString
        let createStringToType() = createTypeToString()    |> Codec.reverse

        let createTypeToBytes() = createTypeToJsonValue() |> Codec.concatenate JsonValueToBytes
        let createBytesToType() = createTypeToBytes()     |> Codec.reverse