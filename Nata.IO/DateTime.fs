namespace Nata.IO

open System
open FSharp.Data

module DateTime =
        
    type Unix = int64

    let epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let toUnix (date:DateTime) : Unix =
        int64 (date.ToUniversalTime() - epoch).TotalSeconds

    let ofUnix (unix:Unix) : DateTime =
        epoch.Add(TimeSpan.FromSeconds (float unix))

    let ofOffset (offset:DateTimeOffset) =
        offset.UtcDateTime

    let toJsonValue (date:DateTime) =
        JsonValue.String (date.ToUniversalTime().ToString("o"))

    let ofJsonValue (text:JsonValue) = 
        text.AsDateTime()

    let toLocal (x:DateTime) = x.ToLocalTime()

    let toUtc (x:DateTime) = x.ToUniversalTime()
        
    module Resolution =

        let year (x:DateTime) =
            new DateTime(x.Year,0,0,0,0,0,0,x.Kind)

        let month (x:DateTime) =
            new DateTime(x.Year,x.Month,0,0,0,0,0,x.Kind)

        let day (x:DateTime) =
            new DateTime(x.Year,x.Month,x.Day,0,0,0,0,x.Kind)

        let hour (x:DateTime) =
            new DateTime(x.Year,x.Month,x.Day,x.Hour,0,0,0,x.Kind)

        let minute (x:DateTime) =
            new DateTime(x.Year,x.Month,x.Day,x.Hour,x.Minute,0,0,x.Kind)

        let second (x:DateTime) =
            new DateTime(x.Year,x.Month,x.Day,x.Hour,x.Minute,x.Second,0,x.Kind)

        let ms (x:DateTime) =
            new DateTime(x.Year,x.Month,x.Day,x.Hour,x.Minute,x.Second,x.Millisecond,x.Kind)

    module Codec =

        open JsonValue.Codec

        let DateTimeToUnix : Codec<DateTime,Unix> = toUnix, ofUnix
        let UnixToDateTime : Codec<Unix,DateTime> = ofUnix, toUnix

        let DateTimeToJson : Codec<DateTime,JsonValue> = toJsonValue, ofJsonValue
        let JsonToDateTime : Codec<JsonValue,DateTime> = ofJsonValue, toJsonValue

        let DateTimeToString = DateTimeToJson |> Codec.concatenate JsonValueToString
        let StringToDateTime = DateTimeToString |> Codec.reverse