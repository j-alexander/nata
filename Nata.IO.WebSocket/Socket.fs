namespace Nata.IO.WebSocket

module Socket =

    open System
    open System.IO
    open System.Threading
    open WebSocket4Net

    open Nata.IO

    type Settings = {
        Host : string
        AutoPingInterval : int option
    }

    type Status<'T> =
        | Opened
        | Received of 'T
        | Closed of exn option

    let inline private connectUsing (sender, receiver) : Source<Settings,'T,unit> =
        function
        | { Host=host; AutoPingInterval=autoPing } ->

            let multicast = new Multicast<Status<'T>>()
            let socket = new WebSocket(host)

            match autoPing with
            | None ->
                socket.EnableAutoSendPing <- false
            | Some interval ->
                socket.EnableAutoSendPing <- true
                socket.AutoSendPingInterval <- interval
            
            socket.Opened.AddHandler(fun _ _ -> multicast.Publish(Opened))
            socket.Closed.AddHandler(fun _ _ -> multicast.Publish(Closed(None)))
            socket.Error.AddHandler(fun _ e -> multicast.Publish(Closed(Some e.Exception)))
            socket |> receiver multicast
               
            let initialize =
                new Lazy<unit>((fun () ->
                    let events = multicast.Subscribe()
                    socket.Open()
                    events
                    |> Seq.takeWhile(function Status.Opened -> false | _ -> true)
                    |> Seq.iter ignore),
                    true)

            let write(message:Event<'T>) =
                initialize.Force()
                message |> Event.data |> sender socket

            let listen() : seq<Event<'T>> =
                let events = multicast.Subscribe()
                initialize.Force()
                seq {
                    (* // disposal, if implemented, would also close any write channels:
                    use _ = { new IDisposable with override x.Dispose() = socket.Close() }
                    *)
                    for event in events do
                        match event with
                        | Status.Opened
                        | Status.Closed(None) -> ()
                        | Status.Closed(Some exn) ->
                            raise exn
                        | Status.Received(message) ->
                            yield Event.create message
                }

            [
                Writer <| write

                Subscriber <| listen
            ]


    let connect : Source<Settings,string,unit> =

        let receiver (multicast:Multicast<Status<string>>)
                     (socket:WebSocket)=
            socket.MessageReceived.AddHandler(fun _ e ->
                multicast.Publish(Received(e.Message)))

        let sender (socket:WebSocket) (message:string) =
            socket.Send(message)

        connectUsing(sender,receiver)


    let connectBinary : Source<Settings,byte[],unit> =

        let receiver (multicast:Multicast<Status<byte[]>>)
                     (socket:WebSocket) =
            socket.DataReceived.AddHandler(fun _ e ->
                multicast.Publish(Received(e.Data)))

        let sender (socket:WebSocket) (message:byte[]) =
            socket.Send(message,0,message.Length)

        connectUsing(sender,receiver)
