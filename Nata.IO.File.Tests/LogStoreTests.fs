namespace Nata.IO.File.Tests

open System.IO
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Capability
open Nata.IO.File

[<TestFixture(Description="File-LogStore")>]
type LogStoreTests() =
    inherit Nata.IO.Tests.LogStoreTests()
        
    let channel() =
        Path.GetTempFileName()
    let connect() = 
        Stream.connect()
        |> Source.mapData JsonValue.Codec.JsonValueToBytes

    override x.Connect() = connect()
    override x.Channel() = channel()