namespace Nata.IO.CosmosDB.Tests

open NUnit.Framework
open FSharp.Data
open Nata.Core
open Nata.IO
open Nata.IO.CosmosDB

[<TestFixture>]
type DocumentTests() = 

    [<Test>]
    member x.TestConnect() =
        let channel : Channel<JsonValue, string> = Document.connect <| guid()
        let expected : Channel<JsonValue, string> = []
        Assert.AreEqual(expected, channel)