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
open Nata.Fun.JsonPath.JsonValue.Query

[<TestFixture>]
type JsonValueTests() =

    [<Test>]
    member x.ParseExamples() =
        // http://goessner.net/articles/JsonPath/
        // https://jsonpath.curiousconcept.com/
        let examples =
            [ "$.store.book[*].author",
              [Exact,Property("store");Exact,Property("book");Exact,Array(Index.Wildcard);Exact,Property("author")]

              "$..author",
              [Any,Property("author")]

              "$.store.*",
              [Exact,Property("store");Exact,Property("*")]

              "$.store..price",
              [Exact,Property("store");Any,Property("price")]

              "$..book[2]",
              [Any,Property("book");Exact,Array(Index.Literal[2])]

              "$..book[(@.length-1)]",
              [Any,Property("book");Exact,Array(Index.Expression "(@.length-1)")]

              "$..book[-1:]",
              [Any,Property("book");Exact,Array(Index.Slice(Some -1,None,1))]

              "$..book[:2]",
              [Any,Property("book");Exact,Array(Index.Slice(None,Some 2,1))]

              "$..book[1:2]",
              [Any,Property("book");Exact,Array(Index.Slice(Some 1,Some 2,1))]

              "$..book[::1]",
              [Any,Property("book");Exact,Array(Index.Slice(None,None,1))]

              "$..book[1:2:3]",
              [Any,Property("book");Exact,Array(Index.Slice(Some 1,Some 2,3))]

              "$..book[0,1]",
              [Any,Property("book");Exact,Array(Index.Literal [0;1])]

              "$..book[?(@.isbn)]",
              [Any,Property("book");Exact,Array(Index.Expression "?(@.isbn)")]

              "$..book[?(@.price<10)]",
              [Any,Property("book");Exact,Array(Index.Expression "?(@.price<10)")]

              "$..*",
              [Any,Property("*")]

              "$.store.book[*]",
              [Exact,Property("store");Exact,Property("book");Exact,Array(Index.Wildcard)]

              "$.store.book[*][*]",
              [Exact,Property("store");Exact,Property("book");Exact,Array(Index.Wildcard);Exact,Array(Index.Wildcard)]
            ]

        for i, example, expected in examples |> Seq.mapi (fun i (e,x) -> i,e,x) do
            Assert.AreEqual(levelsFor example, expected, sprintf "Example #%d" i)

    [<Test>]
    member x.FindExactAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 2m ],
            JsonValue.Parse """{"b":1,"a":2}"""
            |> JsonValue.findList "$.a")

    [<Test>]
    member x.FindExact1stGenerationChild() =
        Assert.AreEqual(
            [ JsonValue.Number 3m ],
            JsonValue.Parse """{"b":1,"a":{"c":3,"d":4}}"""
            |> JsonValue.findList "$.a.c")

    [<Test>]
    member x.FindExact2stGenerationChild() =
        Assert.AreEqual(
            [ JsonValue.Number 5m ],
            JsonValue.Parse """{"b":1,"a":{"c":{"e":5},"d":4}}"""
            |> JsonValue.findList "$.a.c.e")

    [<Test>]
    member x.FindAny1Level() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"b":1,
                 "a":{"c":{"e":5},
                      "b":4}}
            """
            |> JsonValue.findList "$..b")
        Assert.AreEqual(
            [ JsonValue.Parse """{"f":{"b":6,"g":2}}"""
              JsonValue.Number 6m
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"b":{"f":{"b":6,"g":2}},
                 "a":{"c":{"e":5},
                      "b":4}}
            """
            |> JsonValue.findList "$..b")
            
    [<Test>]
    member x.FindAny2Level() =
        Assert.AreEqual(
            [ JsonValue.Number 5m
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"a":{"d":3,
                           "b":5}},
                 "e":{"a":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$..a.b")
        Assert.AreEqual(
            [ JsonValue.Parse """
                { "b":{
                   "a":{
                    "b":{
                     "c":2
                }}}}
              """
              JsonValue.Parse """
                { "c":2 }
              """ ],
            JsonValue.Parse """
                {"a":{
                  "b":{
                   "b":{
                    "a":{
                     "b":{
                      "c":2
                }}}}}}
            """
            |> JsonValue.findList "$..a.b")
        Assert.AreEqual(
            [ JsonValue.Parse """
                { "a":{
                   "a":{
                    "b":{
                     "c":2
                }}}}
              """
              JsonValue.Parse """
                { "c":2 }
              """ ],
            JsonValue.Parse """
                {"a":{
                  "b":{
                   "a":{
                    "a":{
                     "b":{
                      "c":2
                }}}}}}
            """
            |> JsonValue.findList "$..a.b")
        
    [<Test>]
    member x.FindWildcardAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m 
              JsonValue.Parse """{"a":{"d":3,"b":5}}"""
              JsonValue.Parse """{"a":{"a":{"b":4}}}""" ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"a":{"d":3,
                           "b":5}},
                 "e":{"a":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$.*")
        
    [<Test>]
    member x.FindWildcardAt1stGenerationChild() =
        Assert.AreEqual(
            [ JsonValue.Number 6m
              JsonValue.Parse """{"d":3,"b":5}""" ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$.c.*")
        
    [<Test>]
    member x.FindWildcardAt2ndGenerationChild() =
        Assert.AreEqual(
            [ JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$.c.a.*")

    [<Test>]
    member x.FindAnyWildcard() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m 
              JsonValue.Parse """{"d":6,"a":{"d":3,"b":5}}"""
              JsonValue.Number 6m 
              JsonValue.Parse """{"d":3,"b":5}"""
              JsonValue.Number 3m 
              JsonValue.Number 5m 
              JsonValue.Parse """{"c":{"a":{"b":4}}}"""
              JsonValue.Parse """{"a":{"b":4}}"""
              JsonValue.Parse """{"b":4}"""
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$..*")

    [<Test>]
    member x.FindAnyWildcard2Level() =
        Assert.AreEqual(
            [ JsonValue.Number 6m 
              JsonValue.Parse """{"d":3,"b":5}"""
              JsonValue.Parse """{"b":4}""" ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$..c.*")

    [<Test>]
    member x.FindAnyWildcard3Level() =
        Assert.AreEqual(
            [ JsonValue.Number 3m
              JsonValue.Number 5m
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$..c.a.*")

    [<Test>]
    member x.FindAnyWildcardWithChild() =
        Assert.AreEqual(
            [ JsonValue.Number 5m
              JsonValue.Number 4m ],
            JsonValue.Parse """
                {"a":1,
                 "b":2,
                 "c":{"d":6,
                      "a":{"d":3,
                           "b":5}},
                 "e":{"c":{"a":{"b":4}}}
                 }
            """
            |> JsonValue.findList "$..c.*.b")
            
    [<Test>]
    member x.IdentityOfRecord() =
        let record = JsonValue.Parse """
            {"b":1,
             "a":{"c":{"e":5},
                  "b":4}}
        """
        Assert.AreEqual([record], JsonValue.findList "$." record)

    [<Test>]
    member x.IdentityOfArray() =
        let array = JsonValue.Parse """[{"a":3}]"""
        Assert.AreEqual([array], JsonValue.findList "$." array)

    [<Test>]
    member x.FindRootArray() =
        Assert.AreEqual(
            [ JsonValue.String "abc" ],
            JsonValue.Parse """["abc"]"""
            |> JsonValue.findList "$.[*]")

    [<Test>]
    member x.FindRootArrayChild() =
        Assert.AreEqual(
            [ JsonValue.Number 3m ],
            JsonValue.Parse """[{"a":3}]"""
            |> JsonValue.findList "$.[*].a")

    [<Test>]
    member x.FindArrayAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Parse """[1,2,3]""" ],
            JsonValue.Parse """{"a":4,"b":[1,2,3],"c":{"b":[5,6]}}"""
            |> JsonValue.findList "$.b")

    [<Test>]
    member x.FindArrayChildrenAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m
              JsonValue.Number 3m ],
            JsonValue.Parse """{"a":4,"b":[1,2,3],"c":{"b":[5,6]}}"""
            |> JsonValue.findList "$.b[*]")

    [<Test>]
    member x.FindArrayOfArrayChildren() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m
              JsonValue.Number 3m
              JsonValue.Number 4m ],
            JsonValue.Parse """{"a":4,"b":[[1],[2],[3,4]],"c":{"b":[5,6]}}"""
            |> JsonValue.findList "$.b[*][*]")

    [<Test>]
    member x.FindAllArrayChildren() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m
              JsonValue.Number 3m
              JsonValue.Number 5m
              JsonValue.Number 6m ],
            JsonValue.Parse """{"a":4,"b":[1,2,3],"c":{"b":[5,6]}}"""
            |> JsonValue.findList "$..b[*]")

    [<Test>]
    member x.FindAllArrayOfArrayChildren() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m
              JsonValue.Number 3m
              JsonValue.Number 4m
              JsonValue.Number 5m
              JsonValue.Number 6m ],
            JsonValue.Parse """{"a":4,"b":[[1],[2],[3,4]],"c":{"b":[[5],[6]]}}"""
            |> JsonValue.findList "$..b[*][*]")
            
    [<Test>]
    member x.FindLiteralAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """[1,2,3,4,5]"""
            |> JsonValue.findList "$.[0,2]")

    [<Test>]
    member x.FindNegativeLiteralAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """[1,2,3,4,5]"""
            |> JsonValue.findList "$.[0,2,-1]")

    [<Test>]
    member x.FindLiteral1stGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """{"a":[1,2,3,4,5]}"""
            |> JsonValue.findList "$.a[0,2]")

    [<Test>]
    member x.FindNegativeLiteral1stGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """{"a":[1,2,3,4,5]}"""
            |> JsonValue.findList "$.a[0,2,-1]")

    [<Test>]
    member x.FindLiteral2ndGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[0,2]")

    [<Test>]
    member x.FindNegativeLiteral2ndGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[0,2,-1]")

    [<Test>]
    member x.FindInvalidLiteral() =
        Assert.AreEqual(
            [ JsonValue.Number 5m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[4,8,9]")

    [<Test>]
    member x.FindNegativeLiteral() =
        Assert.AreEqual(
            [ JsonValue.Number 4m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[-2]")

    [<Test>]
    member x.FindInvalidNegativeLiteral() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 2m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[1,-9,0]")
        
    [<Test>]
    member x.TestArraySlicesWithPositiveStep() =
        let check query list =
            Assert.AreEqual(
                List.map (decimal >> JsonValue.Number) list,
                JsonValue.Parse """[0,1,2,3]"""
                |> JsonValue.findList query)
        check "$.[0:]" [0..3]
        check "$.[1:]" [1..3]
        check "$.[2:]" [2;3]
        check "$.[3:]" [3]
        check "$.[4:]" []

        check "$.[-4:]" [0..3]
        check "$.[-3:]" [1..3]
        check "$.[-2:]" [2;3]
        check "$.[-1:]" [3]

        check "$.[:0]" [0..3]
        check "$.[:1]" [0]
        check "$.[:2]" [0;1]
        check "$.[:3]" [0..2]
        check "$.[:4]" [0..3]
        check "$.[:5]" [0..3]

        check "$.[1:1]" []
        check "$.[1:2]" [1]
        check "$.[1:3]" [1;2]
        check "$.[1:4]" [1..3]
        check "$.[1:5]" [1..3]

        check "$.[0:-1]" [0;1;2]
        check "$.[1:-1]" [1;2]
        check "$.[2:-1]" [2]
        check "$.[3:-1]" []

        check "$.[0:-2]" [0;1]
        check "$.[1:-2]" [1]
        check "$.[2:-2]" []

        check "$.[0:-3]" [0]
        check "$.[1:-3]" []
        
        check "$.[0:0]" [0..3]
        check "$.[1:0]" [1..3]
        check "$.[2:0]" [2;3]
        check "$.[3:0]" [3]
        check "$.[4:0]" []

        check "$.[::1]" [0..3]
        check "$.[::2]" [0;2]
        check "$.[::3]" [0;3]
        check "$.[::4]" [0]

        check "$.[0::1]" [0..3]
        check "$.[0::2]" [0;2]
        check "$.[0::3]" [0;3]
        check "$.[0::4]" [0]

        check "$.[1::1]" [1..3]
        check "$.[1::2]" [1;3]
        check "$.[1::3]" [1]

        check "$.[2::1]" [2..3]
        check "$.[2::2]" [2]
        check "$.[2::3]" [2]

        check "$.[3::1]" [3]
        check "$.[3::2]" [3]
        check "$.[3::3]" [3]
        
        check "$.[0:-1:1]" [0..2]
        check "$.[1:-1:1]" [1;2]
        check "$.[2:-1:1]" [2]
        check "$.[3:-1:1]" []

        check "$.[0:-2:1]" [0;1]
        check "$.[1:-2:1]" [1]
        check "$.[2:-2:1]" []
        
    [<Test>]
    member x.TestForDepth() =
        let document =
            [1..1000000]
            |> List.fold (fun doc i ->
                [| sprintf "%d" i, doc |]
                |> JsonValue.Record) JsonValue.Null
        Assert.AreEqual(
            JsonValue.Parse """{"1":null}""",
            JsonValue.find "$..2" document)

    [<Test>]
    member x.TestForBreadth() =
        let rec document p w = function
            | 0 -> JsonValue.Number(decimal p)
            | h ->
                [| for i in 1..w -> sprintf "%d" i, (document (p*i) w (h-1)) |]
                |> JsonValue.Record
        Assert.AreEqual(
            JsonValue.Number(decimal (99*94)),
            JsonValue.find "$.99.94" (document 1 1000 2))
        Assert.AreEqual(
            JsonValue.Number(decimal (949872)),
            JsonValue.find "$.949872" (document 1 1000000 1))