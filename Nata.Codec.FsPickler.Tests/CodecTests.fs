module Nata.Codec.FsPickler.Tests

open System
open FSharp.Data
open Nata.Core
open Nata.Codec.FsPickler
open NUnit.Framework

let areEqual (l:'T,r:'T) = Assert.AreEqual(l,r)

let utf8 =
    [
        null
        "Hello"
        "🙂"
        "🐈"
        "©2025 Jonathan Leaver"
    ]

[<Test>]
let ``Test UTF8 Text Using Binary``() =
    let codec : Codec<string,byte[]> = Binary.createTypeToBytes()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Binary.ofType)
        areEqual(result, encoded |> Binary.toType)
        areEqual(value, result)

[<Test>]
let ``Test UTF8 Text Using Xml``() =
    let codec : Codec<string,byte[]> = Xml.createTypeToBytes()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Xml.ofTypeToBytes)
        areEqual(result, encoded |> Xml.toTypeOfBytes)
        areEqual(value, result)

    let codec : Codec<string,string> = Xml.createTypeToString()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Xml.ofType)
        areEqual(result, encoded |> Xml.toType)
        areEqual(value, result)

[<Test>]
let ``Test UTF8 Text Using Json``() =
    let codec : Codec<string,byte[]> = Json.createTypeToBytes()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofTypeToBytes)
        areEqual(result, encoded |> Json.toTypeOfBytes)
        areEqual(value, result)

    let codec : Codec<string,string> = Json.createTypeToString()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofTypeToString)
        areEqual(result, encoded |> Json.toTypeOfString)
        areEqual(value, result)

    let codec : Codec<string,JsonValue> = Json.createTypeToJsonValue()
    for value in utf8 do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofType)
        areEqual(result, encoded |> Json.toType)
        areEqual(value, result)

type Record =
    | Text of string
    | Number of decimal
    | Unit
    | Tuple of string * decimal

let records =
    [
        yield! utf8 |> Seq.map Record.Text
        yield! utf8 |> Seq.mapi (fun i x -> Record.Tuple(x, decimal i))
        yield Record.Tuple("Zero", 0m)
        yield Record.Tuple("Point One", 0.1m)
        yield Record.Tuple("Point Nine Nine", 0.99m)
        yield Unit
        yield! [ 1..10 ] |> Seq.map (fun x -> Record.Number(decimal(Math.Pow(0.5, float x))))
    ]

[<Test>]
let ``Test Records Using Binary``() =
    let codec : Codec<Record,byte[]> = Binary.createTypeToBytes()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Binary.ofType)
        areEqual(result, encoded |> Binary.toType)
        areEqual(value, result)

[<Test>]
let ``Test Records Using Xml``() =
    let codec : Codec<Record,byte[]> = Xml.createTypeToBytes()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Xml.ofTypeToBytes)
        areEqual(result, encoded |> Xml.toTypeOfBytes)
        areEqual(value, result)

    let codec : Codec<Record,string> = Xml.createTypeToString()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Xml.ofType)
        areEqual(result, encoded |> Xml.toType)
        areEqual(value, result)

[<Test>]
let ``Test Records Using Json``() =
    let codec : Codec<Record,byte[]> = Json.createTypeToBytes()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofTypeToBytes)
        areEqual(result, encoded |> Json.toTypeOfBytes)
        areEqual(value, result)

    let codec : Codec<Record,string> = Json.createTypeToString()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofTypeToString)
        areEqual(result, encoded |> Json.toTypeOfString)
        areEqual(value, result)

    let codec : Codec<Record,JsonValue> = Json.createTypeToJsonValue()
    for value in records do
        let encoded = value |> Codec.encoder codec
        let result = encoded |> Codec.decoder codec
        areEqual(encoded, value |> Json.ofType)
        areEqual(result, encoded |> Json.toType)
        areEqual(value, result)
