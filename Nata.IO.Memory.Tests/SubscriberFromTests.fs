namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-SubscriberFrom")>]
type SubscriberFromTests() = 
    inherit Nata.IO.Tests.SubscriberFromTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()