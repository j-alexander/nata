namespace Nata.IO.Tests

open NUnit.Framework
open Nata.IO

[<TestFixture>]
type CodecTests() = 

    let add3 : Codec<int,int> = (fun x -> x+3), (fun x -> x-3)
    let add2 : Codec<int,int> = (fun x -> x+2), (fun x -> x-2)

    [<Test>]
    member x.TestEncoder() =
        Assert.AreEqual(5, 2 |> Codec.encoder add3)

    [<Test>]
    member x.TestDecoder() =
        Assert.AreEqual(2, 5 |> Codec.decoder add3)

    [<Test>]
    member x.TestReverse() =
        let sub3 = Codec.reverse add3
        Assert.AreEqual(2, 5 |> Codec.encoder sub3)
        Assert.AreEqual(5, 2 |> Codec.decoder sub3)

    [<Test>]
    member x.TestIdentity() =
        for i in [1..10] do
            Assert.AreEqual(i, (Codec.encoder Codec.Identity) i)
        for i in [1..10] do
            Assert.AreEqual(i, (Codec.decoder Codec.Identity) i)

    [<Test>]
    member x.TestReverseIdentity() =
        let reverseIdentity = Codec.Identity |> Codec.reverse
        for i in [1..10] do
            Assert.AreEqual(i, (Codec.encoder reverseIdentity) i)
        for i in [1..10] do
            Assert.AreEqual(i, (Codec.decoder reverseIdentity) i)

    [<Test>]
    member x.TestConcatenate() =
        let add5 = add3 |> Codec.concatenate add2
        Assert.AreEqual(7, 2 |> Codec.encoder add5)
        Assert.AreEqual(2, 7 |> Codec.decoder add5)

    [<Test>]
    member x.TestStringToBytesToString() =
        let codec = Codec.StringToBytes |> Codec.concatenate Codec.BytesToString
        let value = "abc"
        Assert.AreEqual(value, value |> Codec.encoder codec)
        Assert.AreEqual(value, value |> Codec.decoder codec)

    [<Test>]
    member x.TestBytesToStringToBytes() =
        let codec = Codec.BytesToString |> Codec.concatenate Codec.StringToBytes
        let value = [| 0x1uy; 0x2uy; 0x3uy |]
        Assert.AreEqual(value, value |> Codec.encoder codec)
        Assert.AreEqual(value, value |> Codec.decoder codec)