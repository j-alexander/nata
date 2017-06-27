namespace Nata.IO.Memory.Tests

open System
open Nata.IO.Memory
open NUnit.Framework

[<TestFixture(Description="Memory-LogStore")>]
type LogStoreTests() = 
    inherit Nata.IO.Tests.LogStoreTests()

    override x.Connect() =
        Configuration.channel()
        |> Configuration.connect()