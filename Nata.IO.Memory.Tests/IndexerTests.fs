namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-Indexer")>]
type IndexerTests() = 
    inherit Nata.IO.Tests.IndexerTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()