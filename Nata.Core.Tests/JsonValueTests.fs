namespace Nata.Core.Tests

open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.Core
open Nata.Core.JsonValue

type RecordType = {
    Index : int
    Value : string
}

[<TestFixture>]
type JsonValueTests() = 

    let json = 
        [|
            "number", 3m |> JsonValue.Number
            "record",
            [|
                "text", "value" |> JsonValue.String
                "null", JsonValue.Null
                "array",
                [| 1m..3m
                |] |> Array.map JsonValue.Number
                   |> JsonValue.Array
            |] |> JsonValue.Record
        |] |> JsonValue.Record

    let text = json.ToString(JsonSaveOptions.DisableFormatting)

    let data = text |> Encoding.Default.GetBytes

    let jsonToStringToBytes =
        JsonValue.Codec.JsonValueToString
        |> Codec.concatenate Codec.StringToBytes

    let bytesToStringToJson =
        Codec.BytesToString
        |> Codec.concatenate JsonValue.Codec.StringToJsonValue

    [<Test>]
    member x.TestJsonToString() =
        Assert.AreEqual(text, json |> Codec.encoder JsonValue.Codec.JsonValueToString)

    [<Test>]
    member x.TestJsonOfString() =
        Assert.AreEqual(json, text |> Codec.decoder JsonValue.Codec.JsonValueToString)

    [<Test>]
    member x.TestStringToJson() =
        Assert.AreEqual(json, text |> Codec.encoder JsonValue.Codec.StringToJsonValue)

    [<Test>]
    member x.TestStringOfJson() =
        Assert.AreEqual(text, json |> Codec.decoder JsonValue.Codec.StringToJsonValue)
        
    [<Test>]
    member x.TestJsonToBytes() =
        Assert.AreEqual(data, json |> Codec.encoder JsonValue.Codec.JsonValueToBytes)

    [<Test>]
    member x.TestJsonOfBytes() =
        Assert.AreEqual(json, data |> Codec.decoder JsonValue.Codec.JsonValueToBytes)

    [<Test>]
    member x.TestBytesToJson() =
        Assert.AreEqual(json, data |> Codec.encoder JsonValue.Codec.BytesToJsonValue)

    [<Test>]
    member x.TestBytesOfJson() =
        Assert.AreEqual(data, json |> Codec.decoder JsonValue.Codec.BytesToJsonValue)

    [<Test>]
    member x.TestJsonToStringToBytes() =
        Assert.AreEqual(data, json |> Codec.encoder jsonToStringToBytes)

    [<Test>]
    member x.TestJsonOfStringOfBytes() =
        Assert.AreEqual(json, data |> Codec.decoder jsonToStringToBytes)

    [<Test>]
    member x.TestBytesToStringToJson() =
        Assert.AreEqual(json, data |> Codec.encoder bytesToStringToJson)

    [<Test>]
    member x.TestBytesOfStringOfJson() =
        Assert.AreEqual(data, json |> Codec.decoder bytesToStringToJson)
        
    [<Test>]
    member x.TestRecordToBytesToRecord() =
        let codec : Codec<RecordType,RecordType> =
            Codec.createTypeToBytes() |> Codec.concatenate (Codec.createBytesToType())
        for index in 1..3 do
            let record = { Index=index; Value = (index*index).ToString() }
            Assert.AreEqual(record, record |> Codec.decoder codec)
        
    [<Test>]
    member x.TestRecordToStringToRecord() =
        let codec : Codec<RecordType,RecordType> =
            Codec.createTypeToString() |> Codec.concatenate (Codec.createStringToType())
        for index in 1..3 do
            let record = { Index=index; Value = (index*index).ToString() }
            Assert.AreEqual(record, record |> Codec.decoder codec)
        
    [<Test>]
    member x.TestRecordToJsonValueToRecord() =
        let codec : Codec<RecordType,RecordType> =
            Codec.createTypeToJsonValue() |> Codec.concatenate (Codec.createJsonValueToType())
        for index in 1..3 do
            let record = { Index=index; Value = (index*index).ToString() }
            Assert.AreEqual(record, record |> Codec.decoder codec)