namespace Nata.IO.EventStore.Tests

open NUnit.Framework

[<TestFixture(Description="EventStore-Serialization")>]
type SerializationTests() =
    inherit Nata.IO.Tests.SerializationTests()

    override x.Connect() =
        Configuration.channel()
        |> Configuration.connect()