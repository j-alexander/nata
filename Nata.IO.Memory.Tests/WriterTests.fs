namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-Writer")>]
type WriterTests() = 
    inherit Nata.IO.Tests.WriterTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()