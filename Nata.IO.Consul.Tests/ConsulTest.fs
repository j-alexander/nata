namespace Nata.IO.Consul.Tests
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
open Nata.IO.Consul

[<TestFixture>]
type ConsulTests() =

    let event() =
        Event.create(guid(), Guid.NewGuid().ToByteArray())

    let connect = 
        { Settings.Address="http://127.0.0.1:8500/"
          Settings.DataCenter="dc1" }
        |> Consul.Client.connect

    [<Test>]
    member x.TestReadWrite() =
        let prefix = guid() |> sprintf "%s/"
        let read,write =
            let node = connect prefix
            node |> reader,
            node |> writer

        for i in 1..3 do

            let expect = event()
            write expect   
            let expectKey, expectValue = Event.data expect

            let result = 
                read() 
                |> Seq.filter (Event.data >> fst >> fun k -> k.Contains(expectKey))
                |> Seq.head
            let resultKey, resultValue = Event.data result

            Assert.AreEqual(expectKey, resultKey)
            Assert.AreEqual(expectValue, resultValue)