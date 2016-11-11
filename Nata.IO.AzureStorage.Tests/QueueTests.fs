namespace Nata.IO.AzureStorage.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Queue
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.Core.JsonValue.Codec
open Nata.IO
open Nata.IO.Capability
open Nata.IO.AzureStorage
open Nata.IO.AzureStorage.Queue

[<TestFixture(Description="Azure-Queue")>]
type QueueTests() =
    inherit Nata.IO.Tests.QueueTests<Queue.Name>()
        
    let channel() : Queue.Name = guid()
    let connect() =
        Emulator.Account.connectionString
        |> Account.create
        |> Queue.connect

    override x.Connect() = connect()
    override x.Channel() = channel()
    override x.Stream(c) = c