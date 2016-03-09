namespace Nata.IO.Tests

open System
open System.Text
open FSharp.Data
open NUnit.Framework
open Nata.IO
open Nata.IO.Capability

[<TestFixture>]
type PositionTests() =

    [<Test>]
    member x.MapPositionValueTest() =
        let mapValue = Position.map (fun (x:int) -> x + 2)
        Assert.AreEqual(Position<int>.Start, Position<int>.Start |> mapValue)
        Assert.AreEqual(Position<int>.At 5, Position<int>.At 3 |> mapValue)
        Assert.AreEqual(Position<int>.End, Position<int>.End |> mapValue)

    [<Test>]
    member x.MapPositionTypeTest() =
        let mapType = Position.map (fun (x:int) -> x.ToString())
        Assert.AreEqual(Position<string>.Start, Position<int>.Start |> mapType)
        Assert.AreEqual(Position<string>.At "3", Position<int>.At 3 |> mapType)
        Assert.AreEqual(Position<string>.End, Position<int>.End |> mapType)
        
