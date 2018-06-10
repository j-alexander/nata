namespace Nata.IO.EventStore

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open NLog
open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData
open Nata.Core

module Client =

    type Subscription =
        | Catchup of EventStoreStreamCatchUpSubscription
        | Live of EventStoreSubscription

    let log = LogManager.GetLogger("Nata.IO.EventStore.Client")
    
    let connect(settings:Settings) : IEventStoreConnection =
        let connectionSettings =
            ConnectionSettings
                .Create()
                .SetDefaultUserCredentials(new UserCredentials(settings.User.Name, settings.User.Password))
                .UseCustomLogger(new Logging.EventStoreNLogLogger(log))
                .Build()
        let endpoint =
            let address = 
                settings.Server.Host
                |> Dns.GetHostAddresses
                |> Seq.find(fun x -> x.AddressFamily = AddressFamily.InterNetwork)
            new IPEndPoint(address, settings.Server.Port)
        let connection = EventStoreConnection.Create(connectionSettings, endpoint)
        let connected = new TaskCompletionSource<unit>()
        let completed = new Lazy<unit>((fun _ -> connected.SetResult()), true)
        connection.Connected.Add(fun _ -> completed.Force())

        log.Info(sprintf "%s:%d Connecting" settings.Server.Host settings.Server.Port)
        connection.ConnectAsync()
        |> Task.wait
        connected.Task
        |> Task.wait

        log.Info(sprintf "%s:%d Connected" settings.Server.Host settings.Server.Port)
        connection
