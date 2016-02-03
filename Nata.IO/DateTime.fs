namespace Nata.IO

open System
open FSharp.Data

module DateTime =

    let toJsonValue (date:DateTime) =
        JsonValue.String (date.ToUniversalTime().ToString("o"))

    let ofJsonValue (text:JsonValue) = 
        text.AsDateTime()

    module Codec =

        open JsonValue.Codec

        let DateTimeToJson : Codec<DateTime,JsonValue> = toJsonValue, ofJsonValue
        let JsonToDateTime : Codec<JsonValue,DateTime> = ofJsonValue, toJsonValue

        let DateTimeToString = DateTimeToJson |> Codec.concatenate JsonValueToString
        let StringToDateTime = DateTimeToString |> Codec.reverse