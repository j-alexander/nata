namespace Nata.IO.Memory.Tests

open NUnit.Framework

[<TestFixture(Description="Memory-Subscriber")>]
type SubscriberTests() = 
    inherit Nata.IO.Tests.SubscriberTests()

    override x.Connect() = Configuration.channel() |> Configuration.connect()