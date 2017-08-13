namespace Nata.IO.EventStore

open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open NLog.FSharp
open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData

module Client =

    type Subscription =
        | Catchup of EventStoreStreamCatchUpSubscription
        | Live of EventStoreSubscription

    let log = new Logger()
    
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

        log.Info "%s:%d Connecting" settings.Server.Host settings.Server.Port
        connection.ConnectAsync().Wait()
        connected.Task.Wait()

        log.Info "%s:%d Connected" settings.Server.Host settings.Server.Port
        connection
