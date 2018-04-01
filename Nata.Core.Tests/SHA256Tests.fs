namespace Nata.Core.Tests

open System
open System.Text
open NUnit.Framework
open Nata.Core

[<TestFixture>]
type SHA256Tests() =

    [<Test>]
    member x.TestEmptyInput() =
        Assert.AreEqual(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            SHA256.hash [||])
        Assert.AreEqual(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            SHA256.hash (Encoding.ASCII.GetBytes ""))
        Assert.AreEqual(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            SHA256.hash (Encoding.UTF8.GetBytes ""))

    [<Test>]
    member x.TestCommonExamples() =
        Assert.AreEqual(
            "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592",
            SHA256.hashUTF8 "The quick brown fox jumps over the lazy dog")
        Assert.AreEqual(
            "ef537f25c895bfa782526529a9b63d97aa631564d5d789c2b765448c8635fb6c",
            SHA256.hashUTF8 "The quick brown fox jumps over the lazy dog.")
        Assert.AreEqual(
            "72726d8818f693066ceb69afa364218b692e62ea92b385782363780f47529c21",
            SHA256.hashUTF8 "中文")
        Assert.AreEqual(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            SHA256.hashUTF8 "abc")

    [<Test>]
    member x.TestGeneratedExamples() =
        [ 1..1000 ]
        |> List.map guidBytes
        |> List.iter(fun input ->
            Assert.AreEqual(
                SHA256.reference input,
                SHA256.hash input))

    [<Test>]
    member x.Test32kExample() =
        let input =
            [| 1..2048 |]
            |> Array.collect guidBytes
        Assert.AreEqual(
            SHA256.reference input,
            SHA256.hash input)

    [<Test>]
    member x.TestCompress() =
        let a = 0x87564c0cu
        let b = 0xf1369725u
        let c = 0x82e6d493u
        let d = 0x63a6b509u
        let e = 0xdd9eff54u
        let f = 0xe07c2655u
        let g = 0xa41f32e7u
        let h = 0xc7d25631u

        let k = 0xc67178f2u
        let w = 0x6534ea14u

        let h0,h1,h2,h3,h4,h5,h6,h7 =
            SHA256.compress (a,b,c,d,e,f,g,h) (k,w)

        Assert.AreEqual(0xe620b22bu, h0)
        Assert.AreEqual(0x87564c0cu, h1)
        Assert.AreEqual(0xf1369725u, h2)
        Assert.AreEqual(0x82e6d493u, h3)
        Assert.AreEqual(0xadcef783u, h4)
        Assert.AreEqual(0xdd9eff54u, h5)
        Assert.AreEqual(0xe07c2655u, h6)
        Assert.AreEqual(0xa41f32e7u, h7)