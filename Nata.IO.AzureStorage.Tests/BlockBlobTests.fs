namespace Nata.IO.AzureStorage.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.JsonValue.Codec
open Nata.IO.Capability
open Nata.IO.AzureStorage
open Nata.IO.AzureStorage.Blob.Block

type JsonBlob = {
    text : string
} with
    static member ToBytes : Codec<JsonBlob,byte[]> = createTypeToBytes()
    static member OfBytes : Codec<byte[],JsonBlob> = createBytesToType()

[<TestFixture>]
type BlockBlobTests() =

    let guid() = Guid.NewGuid().ToString("n")

    let container() =
        Emulator.Account.connectionString
        |> Account.create 
        |> Blob.container (guid())

    let event() =
        { JsonBlob.text = guid() }
        |> Event.create
        |> Event.mapData (Codec.encoder JsonBlob.ToBytes)

    [<Test>]
    member x.TestReadWrite() =
        let container, event = container(), event()
        let blob_name = guid()

        do write container blob_name event
        let result =
            read container blob_name
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        Assert.True(result |> Event.name |> Option.isSome)
        Assert.True(result |> Event.createdAt |> Option.isSome)
        Assert.True(result |> Event.tag |> Option.isSome)