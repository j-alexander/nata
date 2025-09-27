namespace Nata.IO.Consul

open FSharp.Data
open Consul
open Nata.Core

type Key = string
type Value = byte[]
type KeyValue = Key * Value

module Codec =

    let StringToValue : Codec<string, Value> = Codec.StringToBytes
    let ValueToString = Codec.reverse StringToValue

    let JsonValueToValue : Codec<JsonValue, Value> = JsonValue.Codec.JsonValueToBytes
    let ValueToJsonValue = Codec.reverse JsonValueToValue

    let KeyStringToKeyValue : Codec<Key*string, KeyValue> =
        let encode, decode = StringToValue
        (fun (k,v) -> k, encode v),
        (fun (k,v) -> k, decode v)
    let KeyValueToKeyString = Codec.reverse KeyStringToKeyValue

    let KeyJsonValueToKeyValue : Codec<Key*JsonValue, KeyValue> =
        let encode, decode = JsonValueToValue
        (fun (k,v) -> k, encode v),
        (fun (k,v) -> k, decode v)
    let KeyValueToKeyJsonValue = Codec.reverse JsonValueToValue
