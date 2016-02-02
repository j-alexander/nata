namespace Nata.IO.File.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

open NUnit.Framework
open Nata.IO

[<TestFixture>]
type StreamTests() =

    [<Test>]
    member x.TestConcurrency() =

        let read,write,close = File.Stream.create (Path.GetTempFileName())
        let format = sprintf "written line %d completed" 
           
        let work =
            seq {
                yield async {
                    [1..100000] |> Seq.iter (format >> write)
                    return 0
                }
                for reader in 1..40 -> async {
                    do! Async.Sleep(10*reader)
                    return
                        read()
                        |> Seq.mapi(fun i line -> Assert.AreEqual(format (1+i), line))
                        |> Seq.length
                }
            }

        let results =
            Async.Parallel work
            |> Async.RunSynchronously
            |> Seq.sum

        close()
        Assert.Greater(results, 0, "should read some valid data")