namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-WriterTo")>]
type WriterToTests() = 
    inherit Nata.IO.Tests.WriterToTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()