module Nata.Codec.FsPickler

open System.Text
open FSharp.Data
open Nata.Core
open MBrace.FsPickler
open MBrace.FsPickler.Json

module Binary =
    let private serializer = FsPickler.CreateBinarySerializer()

    let ofType(t:'T) : byte[] = serializer.Pickle(t,encoding=Encoding.UTF8)
    let toType(xs:byte[]) : 'T = serializer.UnPickle(xs,encoding=Encoding.UTF8)

    let createTypeToBytes() : Codec<'T,byte[]> = ofType, toType

    let createBytesToType() : Codec<byte[],'T> =
        createTypeToBytes()
        |> Codec.reverse

module Xml =
    let private serializer = FsPickler.CreateXmlSerializer(indent = true)

    let ofType(t:'T) = serializer.PickleToString(t)
    let toType(s:string) : 'T = serializer.UnPickleOfString(s)

    let createTypeToString() : Codec<'T,string> = ofType, toType

    let createStringToType() : Codec<string,'T> =
        createTypeToString()
        |> Codec.reverse

    let createTypeToBytes() : Codec<'T,byte[]> =
        createTypeToString()
        |> Codec.concatenate Codec.StringToBytes

    let createBytesToType() : Codec<byte[],'T> =
        createTypeToBytes()
        |> Codec.reverse

    let ofTypeToBytes(t:'T) : byte[] = t |> ofType |> Codec.encoder Codec.StringToBytes
    let toTypeOfBytes(xs:byte[]) : 'T = xs |> Codec.encoder Codec.BytesToString |> toType

module Json =
    let private serializer = FsPickler.CreateJsonSerializer(indent = true)

    let ofTypeToString(t:'T) = serializer.PickleToString(t)
    let toTypeOfString(s:string) : 'T = serializer.UnPickleOfString(s)

    let createTypeToString() : Codec<'T,string> = ofTypeToString, toTypeOfString

    let createStringToType() : Codec<string,'T> =
        createTypeToString()
        |> Codec.reverse

    let createTypeToJsonValue() : Codec<'T,JsonValue> =
        createTypeToString()
        |> Codec.concatenate JsonValue.Codec.StringToJsonValue

    let createJsonValueToType() : Codec<JsonValue,'T> =
        createTypeToJsonValue()
        |> Codec.reverse

    let ofType(t:'T) : JsonValue = t |> ofTypeToString |> JsonValue.ofString
    let toType(j:JsonValue) : 'T = j |> JsonValue.toString |> toTypeOfString

    let createTypeToBytes() : Codec<'T,byte[]> =
        createTypeToString()
        |> Codec.concatenate Codec.StringToBytes

    let createBytesToType() : Codec<byte[],'T> =
        createTypeToBytes()
        |> Codec.reverse

    let ofTypeToBytes(t:'T) : byte[] = t |> ofTypeToString |> Codec.encoder Codec.StringToBytes
    let toTypeOfBytes(xs:byte[]) : 'T = xs |> Codec.encoder Codec.BytesToString |> toTypeOfString