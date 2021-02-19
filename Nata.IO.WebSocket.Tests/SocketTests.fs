namespace Nata.IO.WebSocket.Tests

open System
open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.WebSocket
open Nata.IO.WebSocket.Socket
open NUnit.Framework

[<TestFixture>]
type SocketTests() = 

    let settings = { Host="ws://websocketstest.com:80/service"; AutoPingInterval=None }

    let service = new Host.Service(9998us)
    let echo, countText, countBinary =
        { Host="ws://localhost:9998/echo"; AutoPingInterval=None },
        { Host="ws://localhost:9998/count/text"; AutoPingInterval=None },
        { Host="ws://localhost:9998/count/binary"; AutoPingInterval=None }

    [<SetUp>]
    member x.SetUp() =
        service.Start()

    [<TearDown>]
    member x.TearDown() =
        service.Stop()

    [<Test; MaxTime(10000)>]
    member x.TestTimeService() =
        let subscription, write =
            let socket = Socket.connect settings
            subscribe socket,
            writer socket
        write (Event.create "version,")
        let time =
            subscription
            |> Seq.logi (fun i x -> if i = 0 then write (Event.create "timer,"))
            |> Seq.take 3
            |> Seq.last
            |> Event.data
        Assert.True(time.Contains(DateTime.UtcNow.Year.ToString()))

    [<Test; MaxTime(30000)>]
    member x.TestStringSubscriber() =
        let subscription, write =
            let socket = Socket.connect countText
            subscribe socket,
            writer socket
        write(Event.create(String.Empty))
        let results =
            subscription
            |> Seq.map (Event.data >> Int64.Parse)
            |> Seq.take 5
            |> Seq.toList
        Assert.AreEqual([0L..4L], results)

    [<Test; MaxTime(30000)>]
    member x.TestBinarySubscriber() =
        let subscription, write =
            let socket = Socket.connectBinary countBinary
            subscribe socket,
            writer socket
        write(Event.create([||]))
        let results =
            subscription
            |> Seq.map (Event.data >> (fun xs -> BitConverter.ToInt64(xs, 0)))
            |> Seq.take 5
            |> Seq.toList
        Assert.AreEqual([0L..4L], results)

    [<Test; MaxTime(10000)>]
    member x.TestStringReadWriteEcho() =
        let subscription, write =
            let socket = Socket.connect echo
            subscribe socket,
            writer socket
        let data =
            let random = new Random()
            [| for i in 1..10 -> random.Next().ToString() |]
        for x in data do write(Event.create x)
        let results =
            subscription
            |> Seq.map Event.data
            |> Seq.take data.Length
            |> Seq.toArray
        Assert.AreEqual(data, results)

    [<Test; MaxTime(10000)>]
    member x.TestBinaryReadWriteEcho() =
        let subscription, write =
            let socket = Socket.connectBinary echo
            subscribe socket,
            writer socket
        let data =
            let random = new Random()
            [| for i in 1..10 -> BitConverter.GetBytes(random.Next()) |]
        for x in data do write(Event.create x)
        let results =
            subscription
            |> Seq.map Event.data
            |> Seq.take data.Length
            |> Seq.toArray
        Assert.AreEqual(data, results)
