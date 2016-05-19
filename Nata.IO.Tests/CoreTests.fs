namespace Nata.IO.Tests

open System
open NUnit.Framework
open Nata.IO

type ExpectedDisposalException() = inherit Exception()
type ExpectedEnumerationException() = inherit Exception()

[<TestFixture>]
type CoreTests() =
    
    [<Test>]
    member x.TestEmptySeqMerge() =
        Assert.AreEqual([], Seq.merge [])
        Assert.AreEqual([], Seq.merge [ Seq.empty ])
        Assert.AreEqual([], Seq.merge [ Seq.empty; Seq.empty; Seq.empty ])

    [<Test>]
    member x.TestSeqMerge() =
        let inputs = [ [1..10]; [11..20]; [21..30]; [31..40]; [41..50] ]
        let merged =
            inputs
            |> List.map List.toSeq
            |> Seq.merge
            |> Seq.toList
        for input in inputs do
            let output =
                merged
                |> List.filter (fun x -> input |> List.exists ((=) x))
            Assert.AreEqual(input, output)
        Assert.AreEqual([1..50], List.sort merged)

    [<Test; ExpectedException(typeof<ExpectedDisposalException>)>]
    member x.TestSeqMergeDisposeExceptions() =
        let merged =
            let sequence =
                seq {
                    use willFail =
                        {   new IDisposable with
                                member x.Dispose() =
                                    raise (new ExpectedDisposalException()) }
                    yield! [1..3]
                }
            Seq.merge [ sequence ]

        let enumerator = merged.GetEnumerator()
        Assert.True(enumerator.MoveNext())
        enumerator.Dispose()

    [<Test; ExpectedException(typeof<ExpectedEnumerationException>)>]
    member x.TestSeqMergeExceptions() =
        let merged =
            let sequence =
                seq {
                    raise (new ExpectedEnumerationException())
                    yield! [1..3]
                }
            Seq.merge [ sequence ]

        let enumerator = merged.GetEnumerator()
        Assert.True(enumerator.MoveNext())

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