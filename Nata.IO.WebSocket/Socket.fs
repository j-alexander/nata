namespace Nata.IO.WebSocket

module Socket =

    open System
    open System.IO
    open System.Collections.Concurrent
    open WebSocket4Net

    open Nata.IO

    type Settings = {
        Host : string
        AutoPingInterval : int option
    }

    type Status =
        | Opened
        | Received of string
        | Closed of exn option

    let receive { Host=host; AutoPingInterval=autoPing } =
        seq {
            use received = new BlockingCollection<Status>(100)
            use socket = new WebSocket(host)
        
            match autoPing with
            | None ->
                socket.EnableAutoSendPing <- false
            | Some interval ->
                socket.EnableAutoSendPing <- true
                socket.AutoSendPingInterval <- interval
            
            socket.Opened.AddHandler(fun s e -> received.Add(Opened))
            socket.Closed.AddHandler(fun s e -> received.Add(Closed(None)))
            socket.Error.AddHandler(fun s e -> received.Add(Closed(Some e.Exception)))
            socket.MessageReceived.AddHandler(fun s e ->
                received.Add(Received(e.Message)))

            socket.Open()
        
            let rec loop() =
                seq {
                    match received.Take() with
                    | Closed (None) ->    ()
                    | Closed (Some e) ->  raise e
                    | Opened ->           yield! loop()
                    | Received message -> yield message
                                          yield! loop()
                }
            yield! loop()
        }

    let transmit (messages:seq<string>) { Host=host; AutoPingInterval=autoPing } =

        use received = new BlockingCollection<Status>(100)
        use socket = new WebSocket(host)
        
        match autoPing with
        | None ->
            socket.EnableAutoSendPing <- false
        | Some interval ->
            socket.EnableAutoSendPing <- true
            socket.AutoSendPingInterval <- interval

        socket.Open()
        for message in messages do
            socket.Send(message)

    let listen = receive >> Seq.map Event.create
        
    let write = Event.data >> Seq.singleton >> transmit 

    let connect : Source<Settings,string,unit> =
        fun (settings:Settings) ->
            [
                Writer <| fun message ->
                    write message settings

                Subscriber <| fun () ->
                    listen settings
            ]