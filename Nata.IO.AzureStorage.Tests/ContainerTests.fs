namespace Nata.IO.AzureStorage.Tests

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
open Nata.IO.Capability
open Nata.IO.AzureStorage

[<TestFixture(Description="Azure-Container")>]
type ContainerTests() =

    let account() = Account.create Emulator.Account.connectionString
    let name() = guid()

    [<Test>]
    member x.TestCreateDevelopmentContainer() =
        let container = Blob.Container.create (name()) (account())
        Assert.True(container.Exists())