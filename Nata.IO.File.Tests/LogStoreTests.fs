namespace Nata.IO.File.Tests

open System
open System.IO
open NUnit.Framework
open FSharp.Data

open Nata.Core
open Nata.IO
open Nata.IO.Capability
open Nata.IO.File

type LogStoreTestEnvelope = {
    data : byte[]
}
with
    static member ToData {LogStoreTestEnvelope.data=data} = data

    static member OfData data = {LogStoreTestEnvelope.data=data}

    static member Codec : Codec<JsonValue, byte[]> =
        JsonValue.Codec.createJsonValueToType()
        |> Codec.concatenate (LogStoreTestEnvelope.ToData, LogStoreTestEnvelope.OfData)

[<TestFixture(Description="File-LogStore")>]
type LogStoreTests() =
    inherit Nata.IO.Tests.LogStoreTests()


    let channel() =
        Path.GetTempFileName()
    let connect() = 
        Stream.connect()
        |> Source.mapData LogStoreTestEnvelope.Codec

    override x.Connect() = connect()
    override x.Channel() = channel()