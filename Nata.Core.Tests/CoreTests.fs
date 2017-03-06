namespace Nata.Core.Tests

open System
open NUnit.Framework
open Nata.Core

type ExpectedDisposalException() = inherit Exception()
type ExpectedEnumerationException() = inherit Exception()

[<TestFixture>]
type CoreTests() =
    
    [<Test>]
    member x.TestEmptySeqConsume() =
        Assert.AreEqual([], Seq.consume [])
        Assert.AreEqual([], Seq.consume [ Seq.empty ])
        Assert.AreEqual([], Seq.consume [ Seq.empty; Seq.empty; Seq.empty ])

    [<Test>]
    member x.TestSeqConsume() =
        let inputs = [ [1..10]; [11..20]; [21..30]; [31..40]; [41..50] ]
        let consumed =
            inputs
            |> List.map List.toSeq
            |> Seq.consume
            |> Seq.toList
        for input in inputs do
            let output =
                consumed
                |> List.filter (fun x -> input |> List.exists ((=) x))
            Assert.AreEqual(input, output)
        Assert.AreEqual([1..50], List.sort consumed)

    [<Test>]
    member x.TestSeqConsumeDisposeExceptions() =
        let consumed =
            let sequence =
                seq {
                    use willFail =
                        {   new IDisposable with
                                member x.Dispose() =
                                    raise (new ExpectedDisposalException()) }
                    yield! [1..3]
                }
            Seq.consume [ sequence ]

        let enumerator = consumed.GetEnumerator()
        Assert.True(enumerator.MoveNext())
        Assert.Throws<ExpectedDisposalException>(fun _ ->
            enumerator.Dispose()
        ) |> ignore

    [<Test>]
    member x.TestSeqConsumeExceptions() =
        let consumed =
            let sequence =
                seq {
                    raise (new ExpectedEnumerationException())
                    yield! [1..3]
                }
            Seq.consume [ sequence ]
            
        let enumerator = consumed.GetEnumerator()
        Assert.Throws<ExpectedEnumerationException>(fun _ ->
            enumerator.MoveNext() |> ignore
        ) |> ignore

    [<Test>]
    member x.TestIntSeqMerge() =
        let odd, even =
            [ for i in 1..100 do if i % 2 = 1 then yield i ],
            [ for i in 1..100 do if i % 2 = 0 then yield i ]
        Assert.AreEqual([1..100], Seq.merge odd even)
        Assert.AreEqual([1..100], Seq.merge even odd)

    [<Test>]
    member x.TestTupleSeqMergeBySnd() =
        let random = new Random()
        let values =
            [ for i in 1..100 -> (random.Next(1,100), i) ]
        let odd, even =
            values |> List.filter (snd >> fun i -> i % 2 = 1),
            values |> List.filter (snd >> fun i -> i % 2 = 0)
        
        Assert.AreNotEqual(values, Seq.merge odd even)
        Assert.AreEqual(values, Seq.mergeBy snd odd even)
        Assert.AreEqual(values, Seq.mergeBy snd even odd)

    [<Test>]
    member x.TestIntSeqChanges() =
        let input = [ 1; 1; 2; 2; 1; 3; 4; 5; 6; 7; 7; 1; 2; 2; 2; 3 ]
        let changes =
            input
            |> Seq.changes
            |> Seq.toList
        let expect = [ 1; 2; 1; 3; 4; 5; 6; 7; 1; 2; 3 ]
        Assert.AreEqual(expect, changes)

    [<Test>]
    member x.TestTupleSeqChangesBySnd() =
        let input = [ 1,1; 2,1; 1,2; 2,2; 1,1; 1,3; 1,4; 1,5; 1,6; 1,7; 2,7; 1,1; 1,2; 2,2; 3,2; 1,3 ]
        let changes =
            input
            |> Seq.changesBy snd
            |> Seq.toList
        let expect = [ 1,1; 1,2; 1,1; 1,3; 1,4; 1,5; 1,6; 1,7; 1,1; 1,2; 1,3 ]
        Assert.AreEqual(expect, changes)

    [<Test>]
    member x.TestBetween() =

        let check between input range expected_output =
            let output =
                input
                |> List.map (between range)
                |> Seq.ofList
                |> Seq.distinct
                |> Seq.toList
            Assert.AreEqual(expected_output, output)

        let check input32 range32 expected32 =

            let input64 = input32 |> List.map int64
            let range64 = range32 |> mapFst int64 |> mapSnd int64
            let expected64 = expected32 |> List.map int64
            
            check Int32.between input32 range32 expected32
            check Int64.between input64 range64 expected64

        check [0   .. 10] (  5,  12) [  5 .. 10]
        check [0   .. 10] ( 12,   5) [  5 .. 10]
        check [0   .. 10] ( -5,   2) [  0 ..  2]
        check [0   .. 10] (  2,  -5) [  0 ..  2]
        check [-10 ..  0] ( -5, -12) [-10 .. -5]
        check [-10 ..  0] (-12,  -5) [-10 .. -5]
        check [-10 .. 10] ( -5,   5) [ -5 ..  5]
        check [-10 .. 10] (  5,  -5) [ -5 ..  5]
        check [-10 .. 10] (  5,  -5) [ -5 ..  5]

    [<Test>]
    member x.TestStringTrySubstring() =
        Assert.AreEqual(Some "", "" |> String.trySubstring 0 0)
        Assert.AreEqual(None, "" |> String.trySubstring 0 1)

        Assert.AreEqual(Some "", "abc" |> String.trySubstring 0 0)
        Assert.AreEqual(Some "a", "abc" |> String.trySubstring 0 1)
        Assert.AreEqual(Some "ab", "abc" |> String.trySubstring 0 2)
        Assert.AreEqual(Some "abc", "abc" |> String.trySubstring 0 3)
        Assert.AreEqual(None, "abc" |> String.trySubstring 0 4)

        Assert.AreEqual(Some "", "abc" |> String.trySubstring 1 0)
        Assert.AreEqual(Some "b", "abc" |> String.trySubstring 1 1)
        Assert.AreEqual(Some "bc", "abc" |> String.trySubstring 1 2)
        Assert.AreEqual(None, "abc" |> String.trySubstring 1 3)

        Assert.AreEqual(Some "", "abc" |> String.trySubstring 2 0)
        Assert.AreEqual(Some "c", "abc" |> String.trySubstring 2 1)
        Assert.AreEqual(None, "abc" |> String.trySubstring 2 2)

        Assert.AreEqual(Some "", "abc" |> String.trySubstring 3 0)
        Assert.AreEqual(None, "abc" |> String.trySubstring 3 1)

        Assert.AreEqual(None, "abc" |> String.trySubstring 4 0)

        Assert.AreEqual(None, "abc" |> String.trySubstring -1 0)
        Assert.AreEqual(None, "abc" |> String.trySubstring -1 3)

        Assert.AreEqual(None, "abc" |> String.trySubstring 0 -1)
        Assert.AreEqual(None, "abc" |> String.trySubstring 3 -1)
        
    [<Test>]
    member x.TestStringTryStartAt() =
        Assert.AreEqual(None, "" |> String.tryStartAt -1)
        Assert.AreEqual(Some "", "" |> String.tryStartAt 0)
        
        Assert.AreEqual(None, "abc" |> String.tryStartAt -1)
        Assert.AreEqual(Some "abc", "abc" |> String.tryStartAt 0)
        Assert.AreEqual(Some "bc", "abc" |> String.tryStartAt 1)
        Assert.AreEqual(Some "c", "abc" |> String.tryStartAt 2)
        Assert.AreEqual(Some "", "abc" |> String.tryStartAt 3)
        Assert.AreEqual(None, "abc" |> String.tryStartAt 4)