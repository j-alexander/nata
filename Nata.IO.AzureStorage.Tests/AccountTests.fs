namespace Nata.IO.AzureStorage.Tests

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
open Nata.IO.AzureStorage

[<TestFixture(Description="Azure-Account")>]
type AccountTests() =

    [<Test>]
    member x.TestUseDevelopmentStorage() =
        let account = Account.create Emulator.Account.connectionString
        Assert.IsNotNull(account)
