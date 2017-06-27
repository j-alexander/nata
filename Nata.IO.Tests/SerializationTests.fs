namespace Nata.IO.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.Core.JsonValue.Codec
open Nata.IO
open Nata.IO.Channel

type DataType = {
    case : string
    at : DateTime
} with
    static member ToBytes : Codec<DataType,byte[]> = createTypeToBytes()
    static member OfBytes : Codec<byte[],DataType> = createBytesToType()

[<AbstractClass>]
type SerializationTests() =

    abstract member Connect : unit -> Channel<byte[],int64>

    [<Test>]
    member x.TestSerializationCodecs() =
        let write, read =
            let connection =
                x.Connect()
                |> Channel.mapData DataType.OfBytes
            writer connection,
            reader connection

        let events =
            [ for i in 0..10 ->
                { Data =
                    { DataType.case = sprintf "Event-%d" i
                      DataType.at = DateTime.Now }
                  Metadata =
                    [ Value.Name "SerializationTests"
                      Value.EventType "AutomaticSerialization" ]
                  At = DateTime.UtcNow } ]

        for event in events do
            write event

        for (event, result) in read() |> Seq.zip events do
            Assert.AreEqual(event.Data.case, result.Data.case)