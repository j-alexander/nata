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
open Nata.IO.AzureStorage.Blob.Append

[<TestFixture>]
type AppendBlobTests() =

    let guid() = Guid.NewGuid().ToString("n")

    let container() =
        Emulator.Account.connectionString
        |> Account.create 
        |> Blob.container (guid())

    let event() =
        [| "text", JsonValue.String (guid()) |]
        |> JsonValue.Record
        |> Event.create

    [<Test; Ignore("AppendBlob is not supported by emulator.  Run with Azure only.")>]
    member x.TestReadWrite() =
        let container, event = container(), event()
        let blob_name = guid()

        do write container blob_name event
        let result =
            read container blob_name
            |> Seq.head

        Assert.AreEqual(event.Data, result.Data)
        Assert.True(result |> Event.name |> Option.isSome)
        