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

    [<Test>]
    member x.TestReadWriteFrom() =
        let writeTo, readFrom =
            let container, blob_name = container(), guid()
            writeTo container blob_name,
            readFrom container blob_name

        let writeEvent = event()
        let writePosition = writeTo Position.Start writeEvent
        Assert.False(writePosition |> String.IsNullOrWhiteSpace)

        let readEvent, readPosition =
            readFrom (Position.At writePosition)
            |> Seq.head
        Assert.False(readPosition |> String.IsNullOrWhiteSpace)
        Assert.AreEqual(writePosition, readPosition)
        Assert.AreEqual(writeEvent.Data, readEvent.Data)

        let readTag = Event.tag readEvent
        Assert.True(readTag.IsSome)
        Assert.AreEqual(readTag, Some writePosition)
        Assert.AreEqual(readTag, Some readPosition)
        
    [<Test>]
    member x.TestReadFromTagWhenTagExpires() =
        let writeTo, readFrom =
            let container, blob_name = container(), guid()
            writeTo container blob_name,
            readFrom container blob_name

        let oldEvent = event()
        let oldWritePosition = writeTo Position.End oldEvent
        Assert.False(oldWritePosition |> String.IsNullOrWhiteSpace)

        let reader = readFrom (Position.At oldWritePosition)
        let enumerator = reader.GetEnumerator()

        Assert.True(enumerator.MoveNext())
        let readEvent, readPosition = enumerator.Current
        Assert.AreEqual(oldWritePosition, readPosition)
        Assert.AreEqual(oldEvent.Data, readEvent.Data)

        Assert.True(enumerator.MoveNext())
        let readEvent, readPosition = enumerator.Current
        Assert.AreEqual(oldWritePosition, readPosition)
        Assert.AreEqual(oldEvent.Data, readEvent.Data)

        let newEvent = event()
        let newWritePosition = writeTo Position.End newEvent
        Assert.False(newWritePosition |> String.IsNullOrWhiteSpace)
        Assert.AreNotEqual(newWritePosition, oldWritePosition)

        Assert.False(enumerator.MoveNext(), "we should no longer be able to read from this position")
        
    [<Test>]
    member x.TestReadFromAnyWhenTagExpires() =
        let writeTo, readFrom =
            let container, blob_name = container(), guid()
            writeTo container blob_name,
            readFrom container blob_name

        let oldEvent = event()
        let oldWritePosition = writeTo Position.End oldEvent
        Assert.False(oldWritePosition |> String.IsNullOrWhiteSpace)

        let reader = readFrom Position.End
        let enumerator = reader.GetEnumerator()

        Assert.True(enumerator.MoveNext())
        let readEvent, readPosition = enumerator.Current
        Assert.AreEqual(oldWritePosition, readPosition)
        Assert.AreEqual(oldEvent.Data, readEvent.Data)

        let newEvent = event()
        let newWritePosition = writeTo Position.End newEvent
        Assert.False(newWritePosition |> String.IsNullOrWhiteSpace)
        Assert.AreNotEqual(newWritePosition, oldWritePosition)

        Assert.True(enumerator.MoveNext())
        let readEvent, readPosition = enumerator.Current
        Assert.AreEqual(newWritePosition, readPosition)
        Assert.AreEqual(newEvent.Data, readEvent.Data)

        Assert.True(enumerator.MoveNext())
        let readEvent, readPosition = enumerator.Current
        Assert.AreEqual(newWritePosition, readPosition)
        Assert.AreEqual(newEvent.Data, readEvent.Data)
        
    [<Test>]
    member x.TestReadFromFalsePosition() =
        let writeTo, readFrom =
            let container, blob_name = container(), guid()
            writeTo container blob_name,
            readFrom container blob_name

        let event = event()
        let writePosition = writeTo Position.End event
        Assert.False(writePosition |> String.IsNullOrWhiteSpace)

        let falsePosition = guid()
        let reader = readFrom (Position.At falsePosition)
        Assert.IsEmpty(reader)
        
    [<Test>]
    member x.TestReadFromABlobWithNoData() =
        let readFrom =
            let container, blob_name = container(), guid()
            readFrom container blob_name
        
        let reader = readFrom Position.Start
        Assert.IsEmpty(reader)
