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
              [Exists,Property("store");Exists,Property("book");Exists,Array(Predicate.Wildcard);Exists,Property("author")]

              "$..author",
              [All,Property("author")]

              "$.store.*",
              [Exists,Property("store");Exists,Property("*")]

              "$.store..price",
              [Exists,Property("store");All,Property("price")]

              "$..book[2]",
              [All,Property("book");Exists,Array(Predicate.Index[2])]

              "$..book[(@.length-1)]",
              [All,Property("book");Exists,Array(Predicate.Expression "(@.length-1)")]

              "$..book[-1:]",
              [All,Property("book");Exists,Array(Predicate.Slice(Some -1,None,1))]

              "$..book[:2]",
              [All,Property("book");Exists,Array(Predicate.Slice(None,Some 2,1))]

              "$..book[1:2]",
              [All,Property("book");Exists,Array(Predicate.Slice(Some 1,Some 2,1))]

              "$..book[::1]",
              [All,Property("book");Exists,Array(Predicate.Slice(None,None,1))]

              "$..book[1:2:3]",
              [All,Property("book");Exists,Array(Predicate.Slice(Some 1,Some 2,3))]

              "$..book[0,1]",
              [All,Property("book");Exists,Array(Predicate.Index [0;1])]

              "$..book[?(@.isbn)]",
              [All,Property("book");Exists,Array(Predicate.Expression "?(@.isbn)")]

              "$..book[?(@.price<10)]",
              [All,Property("book");Exists,Array(Predicate.Expression "?(@.price<10)")]

              "$..*",
              [All,Property("*")]

              "$.store.book[*]",
              [Exists,Property("store");Exists,Property("book");Exists,Array(Predicate.Wildcard)]

              "$.store.book[*][*]",
              [Exists,Property("store");Exists,Property("book");Exists,Array(Predicate.Wildcard);Exists,Array(Predicate.Wildcard)]
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
    member x.FindAll1Level() =
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
    member x.FindAll2Level() =
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
    member x.FindAllWildcard() =
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
    member x.FindAllWildcard2Level() =
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
    member x.FindAllWildcard3Level() =
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
    member x.FindAllWildcardWithChild() =
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
    member x.FindIndexAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """[1,2,3,4,5]"""
            |> JsonValue.findList "$.[0,2]")

    [<Test>]
    member x.FindNegativeIndexAtRoot() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """[1,2,3,4,5]"""
            |> JsonValue.findList "$.[0,2,-1]")

    [<Test>]
    member x.FindIndex1stGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """{"a":[1,2,3,4,5]}"""
            |> JsonValue.findList "$.a[0,2]")

    [<Test>]
    member x.FindNegativeIndex1stGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """{"a":[1,2,3,4,5]}"""
            |> JsonValue.findList "$.a[0,2,-1]")

    [<Test>]
    member x.FindIndex2ndGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[0,2]")

    [<Test>]
    member x.FindNegativeIndex2ndGeneration() =
        Assert.AreEqual(
            [ JsonValue.Number 1m
              JsonValue.Number 3m
              JsonValue.Number 5m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[0,2,-1]")

    [<Test>]
    member x.FindInvalidIndex() =
        Assert.AreEqual(
            [ JsonValue.Number 5m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[4,8,9]")

    [<Test>]
    member x.FindNegativeIndex() =
        Assert.AreEqual(
            [ JsonValue.Number 4m ],
            JsonValue.Parse """{"a":{"b":[1,2,3,4,5]}}"""
            |> JsonValue.findList "$.a.b[-2]")
    [<Test>]
    member x.FindInvalidNegativeIndex() =
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
        
