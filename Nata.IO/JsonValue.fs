namespace Nata.IO

open System.Text
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    let toString (json:JsonValue) = json.ToString(JsonSaveOptions.DisableFormatting)
    let toBytes =  toString >> Encoding.Default.GetBytes

    let ofString (json:string) = JsonValue.Parse json
    let ofBytes = Encoding.Default.GetString >> ofString

    module Codec =

        let JsonValueToString : Codec<JsonValue,string> = toString, ofString
        let StringToJsonValue : Codec<string,JsonValue> = ofString, toString

        let JsonValueToBytes : Codec<JsonValue,byte[]> = toBytes, ofBytes
        let BytesToJsonValue : Codec<byte[],JsonValue> = ofBytes, toBytes