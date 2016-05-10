namespace Nata.IO.RabbitMQ.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.JsonValue.Codec
open Nata.IO.Capability
open Nata.IO.RabbitMQ

[<TestFixture(Description="RabbitMQ-Queue")>]
type QueueTests() =
    inherit Nata.IO.Tests.QueueTests<Queue.Exchange*Queue.Name>()

    let channel() : Queue.Exchange * Queue.Name = "", Guid.NewGuid().ToString("n")
    let connect() = Queue.connect "localhost"

    override x.Connect() = connect()
    override x.Channel() = channel()
    override x.Stream(c) = snd c