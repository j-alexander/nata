namespace Nata.IO.File.Tests

open System
open System.IO
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability
open Nata.IO.File
open Nata.IO.File.Stream

module AcceptanceTests =


    let connect() = 
        Stream.connect()
        |> Source.map JsonValue.Codec.JsonValueToBytes
                      Codec.Identity
    

    [<TestFixture>]
    type FileChannelTests() =
        inherit Nata.IO.Tests.ChannelTests()
        override x.Connect() = connect()
        override x.Channel() = Path.GetTempFileName()