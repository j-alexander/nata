namespace Nata.IO.Tests

open System
open NUnit.Framework
open Nata.IO

type ExpectedDisposalException() = inherit Exception()

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
        let sequence =
            seq {
                use willFail =
                    {   new IDisposable with
                            member x.Dispose() =
                                raise (new ExpectedDisposalException()) }
                yield [1..3]
            }
        let enumerator = sequence.GetEnumerator()
        Assert.True(enumerator.MoveNext())
        enumerator.Dispose()