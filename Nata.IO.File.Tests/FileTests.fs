namespace Nata.IO.File.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability
open Nata.IO.File
open Nata.IO.File.Stream

[<TestFixture(Description="File")>]
type FileTests() =

    let date = DateTime.UtcNow
    let event fn i =
        Event.createAt date (JsonValue.Number i)
        |> Event.withName "event_name"
        |> Event.withEventType fn
        |> Event.withStream "Nata.IO.File.Tests"

    let connect() = 
        Stream.create(Path.GetTempFileName())

    [<Test>]
    member x.TestConcurrency() =
        let stream = connect()
        let read,write,listen =
            Nata.IO.Capability.reader stream,
            Nata.IO.Capability.writer stream,
            Nata.IO.Capability.subscriber stream
             
        let format = event "StreamTests.TestConcurrency"
           
        let work =
            seq {
                yield async {
                    Assert.AreEqual(10000,
                        listen()
                        |> Seq.mapi(fun i actual ->
                            let expected = i |> decimal |> (+) 1m |> format
                            Assert.AreEqual(expected, actual))
                        |> Seq.take 10000
                        |> Seq.length)
                    return 0
                }
                yield async {
                    [1m..10000m] |> Seq.iter (format >> write)
                    return 0
                }
                for reader in 1..40 -> async {
                    do! Async.Sleep(10*reader)
                    return
                        read()
                        |> Seq.mapi(fun i actual -> 
                            let expected = i |> decimal |> (+) 1m |> format
                            Assert.AreEqual(expected, actual))
                        |> Seq.length
                }
            }

        let results =
            Async.Parallel work
            |> Async.RunSynchronously
            |> Seq.sum

        Assert.Greater(results, 0, "should read some valid data")