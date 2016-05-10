namespace Nata.IO.Memory.Tests

open NUnit.Framework
open Nata.IO.Memory

[<TestFixture(Description="Memory-Source")>]
type SourceTests() = 
    inherit Nata.IO.Tests.SourceTests()

    override x.Connect() = Configuration.connect()
    override x.Channel() = Configuration.channel()