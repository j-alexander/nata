namespace Nata.IO.EventStore.Tests

open NUnit.Framework

[<TestFixture(Description="EventStore-LogStore")>]
type LogStoreTests() =
    inherit Nata.IO.Tests.LogStoreTests()

    override x.Connect() =
        Configuration.channel()
        |> Configuration.connect()
