namespace Nata.Core

open System
open FSharp.Data

module DateTime =
        
    type Unix = int64

    let epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let toUnixSeconds (date:DateTime) : Unix =
        int64 (date.ToUniversalTime() - epoch).TotalSeconds

    let ofUnixSeconds (unix:Unix) : DateTime =
        epoch.Add(TimeSpan.FromSeconds (float unix))

    let toUnixMilliseconds (date:DateTime) : Unix =
        int64 (date.ToUniversalTime() - epoch).TotalMilliseconds

    let ofUnixMilliseconds (unix:Unix) : DateTime =
        epoch.Add(TimeSpan.FromMilliseconds (float unix))

    let ofOffset (offset:DateTimeOffset) =
        offset.UtcDateTime

    let ofString (x:string) =
        match DateTime.TryParse x with
        | true, date -> Some date
        | _ -> None

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

        let DateTimeToUnixSeconds : Codec<DateTime,Unix> = toUnixSeconds, ofUnixSeconds
        let UnixSecondsToDateTime : Codec<Unix,DateTime> = ofUnixSeconds, toUnixSeconds

        let DateTimeToUnixMilliseconds : Codec<DateTime,Unix> = toUnixMilliseconds, ofUnixMilliseconds
        let UnixMillisecondsToDateTime : Codec<Unix,DateTime> = ofUnixMilliseconds, toUnixMilliseconds

        let DateTimeToJson : Codec<DateTime,JsonValue> = toJsonValue, ofJsonValue
        let JsonToDateTime : Codec<JsonValue,DateTime> = ofJsonValue, toJsonValue

        let DateTimeToString = DateTimeToJson |> Codec.concatenate JsonValueToString
        let StringToDateTime = DateTimeToString |> Codec.reverse