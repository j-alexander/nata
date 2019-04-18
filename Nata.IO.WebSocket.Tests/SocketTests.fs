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
        let write, subscribe =
            let socket = Socket.connect settings
            writer socket,
            subscriber socket
        let subscription =
            subscribe()
        write (Event.create "version,")
        let time =
            subscription
            |> Seq.logi (fun i x -> if i = 0 then write (Event.create "timer,"))
            |> Seq.take 3
            |> Seq.last
            |> Event.data
        Assert.True(time.Contains(DateTime.UtcNow.Year.ToString()))