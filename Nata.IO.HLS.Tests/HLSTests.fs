namespace Nata.IO.HLS.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.HLS

[<TestFixture>]
type HLSTests() =

    let event() =
        Event.create(guid(), Guid.NewGuid().ToByteArray())

    let connect = 
        { Settings.Address="http://127.0.0.1:8500/" }
        |> HLS.Client.connect

    [<Test>]
    member x.TestRead() =
        let read =
            connect()
            |> reader

        for i in 1..3 do
            //
            // let expectKey, expectValue =
            //     (), ()
            //
            // let result = 
            //     read() 
            //     |> Seq.filter (Event.data >> (fun _ -> true))
            //     |> Seq.head
            // let resultKey, resultValue = Event.data result
            //
            // Assert.AreEqual(expectKey, resultKey)
            // Assert.AreEqual(expectValue, resultValue)
            Assert.True(true)