namespace Nata.IO.EventStore

open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open NLog.FSharp
open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData

module Client =

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
        connection.ConnectAsync().Wait()
        connection
