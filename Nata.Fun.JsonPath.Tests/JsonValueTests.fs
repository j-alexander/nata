namespace Nata.IO.JsonPath

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks

open FSharp.Data
open NUnit.Framework
open Nata.Fun.JsonPath
open Nata.Fun.JsonPath.JsonValue

[<TestFixture>]
type JsonValueTests() =



    [<Test>]
    member x.ParseExamples() =
        // http://goessner.net/articles/JsonPath/
        // https://jsonpath.curiousconcept.com/
        let examples =
            [ "$.store.book[*].author",
              [Exists,Node("store"); Exists,Array("book","[*]"); Exists,Node("author")]

              "$..author",
              [All,Node("author")]

              "$.store.*",
              [Exists,Node("store");Exists,Node("*")]

              "$.store..price",
              [Exists,Node("store");All,Node("price")]

              "$..book[2]",
              [All,Array("book","[2]")]

              "$..book[(@.length-1)]",
              [All,Array("book","[(@.length-1)]")]

              "$..book[-1:]",
              [All,Array("book","[-1:]")]

              "$..book[0,1]",
              [All,Array("book","[0,1]")]

              "$..book[:2]",
              [All,Array("book","[:2]")]

              "$..book[?(@.isbn)]",
              [All,Array("book","[?(@.isbn)]")]

              "$..book[?(@.price<10)]",
              [All,Array ("book","[?(@.price<10)]")]

              "$..*",
              [All,Node("*")]
            ]

        for i, example, expected in examples |> Seq.mapi (fun i (e,x) -> i,e,x) do
            Assert.AreEqual(levelsFor example, expected, sprintf "Example #%d" i)
