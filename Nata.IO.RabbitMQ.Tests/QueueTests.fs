namespace Nata.IO.RabbitMQ.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.Core.JsonValue.Codec
open Nata.IO
open Nata.IO.Capability
open Nata.IO.RabbitMQ

[<TestFixture(Description="RabbitMQ-Queue")>]
type QueueTests() =
    inherit Nata.IO.Tests.QueueTests()

    let exchange : Queue.Exchange = ""
    let connect(name : Queue.Name) =
        Queue.connect "localhost" (exchange,name)

    override x.ConnectWithName() =
        let name = guid() in
        connect name, name