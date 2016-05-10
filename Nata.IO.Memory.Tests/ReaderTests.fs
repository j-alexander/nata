namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-Reader")>]
type ReaderTests() = 
    inherit Nata.IO.Tests.ReaderTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()