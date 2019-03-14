namespace Nata.IO.EventStore.Tests

open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.EventStore

[<TestFixture>]
type MetadataTests() =

    let maxCountOf n =
        let settings, channel =
            Configuration.settings,
            Configuration.channel()
        let metadata = { Metadata.empty with MaxCount=Some (int64 n) }
        let channel =
            Stream.connectWithMetadata settings (channel, metadata)
            |> Channel.mapData Codec.BytesToString
            |> Channel.mapData Codec.StringToInt32
        Channel.readerFrom channel >> Seq.map (fst >> Event.data),
        Event.create >> Channel.writer channel

    [<Test>]
    member x.TestMaxCountOf1() =
        let readFrom, write = maxCountOf 1
        for i in 1..100 do
            write i
            Assert.AreEqual(i, readFrom Position.Start |> Seq.head)
            Assert.AreEqual([i], readFrom Position.Start |> Seq.toList)

    [<Test>]
    member x.TestMaxCountOf2() =
        let readFrom, write = maxCountOf 2
        write 0
        for i in 1..100 do
            write i
            Assert.AreEqual(i-1, readFrom Position.Start |> Seq.head)
            Assert.AreEqual([i-1; i], readFrom Position.Start |> Seq.toList)

    [<Test>]
    member x.TestMaxCountOf100() =
        let readFrom, write = maxCountOf 100
        for i in 1..100 do write i
        for i in 1..100 do
            Assert.AreEqual(i, readFrom Position.Start |> Seq.head)
            write -1
