namespace Nata.IO.Memory.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.IO
open Nata.IO.Capability
open Nata.IO.Memory
open Nata.IO.Memory.Stream

module AcceptanceTests =

    let inline connect() = Stream.connect() (Guid.NewGuid().ToString())

    [<TestFixture>]
    type MemoryWriterTests() = 
        inherit Nata.IO.Tests.WriterTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemoryWriterToTests() = 
        inherit Nata.IO.Tests.WriterToTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemoryReaderTests() =
        inherit Nata.IO.Tests.ReaderTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemoryReaderFromTests() =
        inherit Nata.IO.Tests.ReaderFromTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemorySubscriberTests() =
        inherit Nata.IO.Tests.SubscriberTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemorySubscriberFromTests() =
        inherit Nata.IO.Tests.SubscriberFromTests()
        override x.Connect() = connect()

    [<TestFixture>]
    type MemorySourceTests() =
        inherit Nata.IO.Tests.SourceTests()
        override x.Connect() = Stream.connect()
        override x.Channel() = Guid.NewGuid().ToString("n")