namespace Nata.IO.WebSocket.Tests

open System
open Nata.IO
open Nata.IO.Capability
open Nata.IO.WebSocket
open Nata.IO.WebSocket.Socket
open NUnit.Framework

[<TestFixture>]
type SocketTests() = 

    let settings = { Host="ws://ws.websocketstest.com:80/service"; AutoPingInterval=None }

    [<Test; Timeout(10000)>]
    member x.TestTimeService() =
        let write, subscribe =
            let socket = Socket.connect settings
            writer socket,
            subscriber socket
        write (Event.create "timer,")
        let time =
            subscribe()
            |> Seq.take 3
            |> Seq.last
            |> Event.data
        Assert.True(time.Contains(DateTime.UtcNow.Year.ToString()))