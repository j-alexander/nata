namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-ReaderFrom")>]
type ReaderFromTests() = 
    inherit Nata.IO.Tests.ReaderFromTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()