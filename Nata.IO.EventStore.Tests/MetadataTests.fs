namespace Nata.IO.EventStore.Tests

open System
open System.Threading
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.EventStore

[<TestFixture>]
type MetadataTests() =

    let withMetadata f =
        let settings, channel =
            Configuration.settings,
            Configuration.channel()
        let metadata = f Metadata.empty
        let channel =
            Stream.connectWithMetadata settings (channel, metadata)
            |> Channel.mapData Codec.BytesToString
            |> Channel.mapData Codec.StringToInt32
        Channel.readerFrom channel >> Seq.map (fst >> Event.data),
        Event.create >> Channel.writer channel

    let maxCountOf n =
        withMetadata (fun m -> { m with MaxCount=Some (int64 n) })
    let maxAgeOf age =
        withMetadata (fun m -> { m with MaxAge=Some age })

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

    [<Test>]
    member x.TestMaxAge() =
        let readFrom, write = maxAgeOf (TimeSpan.FromSeconds(5.))

        write 314
        Assert.AreEqual([314], readFrom Position.Start |> Seq.toList)

        Thread.Sleep(7000)
        Assert.AreEqual([], readFrom Position.Start |> Seq.toList, "Should have expired after 10 seconds.")

        write 315
        Assert.AreEqual([315], readFrom Position.Start |> Seq.toList)