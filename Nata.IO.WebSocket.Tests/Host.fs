module Nata.IO.WebSocket.Tests.Host

open System
open System.Net
open System.Text
open System.Threading
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.Successful
open Suave.Operators
open Suave.Writers
open Suave.WebSocket
open Suave.Filters

let countText =
    handShake <| fun ws _ ->
        let rec loop(i) =
            socket {
                do! Async.Sleep(1000)
                    |> Async.map Choice1Of2
                let code = Opcode.Text
                let data =
                    sprintf "%d" i
                    |> Encoding.UTF8.GetBytes
                    |> ByteSegment
                do! ws.send code data true
                return! loop(i+1)
            }
        socket {
            let! _ = ws.read()
            return! loop(0)
        }

let countBinary =
    handShake <| fun ws _ ->
        let rec loop(i:int32) =
            socket {
                do! Async.Sleep(1000)
                    |> Async.map Choice1Of2
                let code = Opcode.Binary
                let data =
                    BitConverter.GetBytes(i)
                    |> ByteSegment
                do! ws.send code data true
                return! loop(i+1)
            }
        socket {
            let! _ = ws.read()
            return! loop(0)
        }

let echo =
    handShake <| fun ws _ ->
        let rec loop() =
            socket {
                let! (code:Opcode, data:byte[], fin:bool) = ws.read()
                match code with
                | Opcode.Close ->
                    return ()
                | Opcode.Text | Opcode.Binary ->
                    do! ws.send code (ByteSegment data) fin
                    return! loop()
                | _ ->
                    return! loop()
            }
        loop()

let api : WebPart =
    choose
        [ GET >=> path "/count/text" >=> countText
          GET >=> path "/count/binary" >=> countBinary
          GET >=> pathStarts "/echo" >=> echo
          GET >=> OK "hello" ]
    >=> setMimeType "application/json; charset=utf-8"
    

type Command =
    | Start
    | Stop

type Service(port:uint16) =

    let service = MailboxProcessor.Start(fun (inbox:MailboxProcessor<Command*AsyncReplyChannel<unit>>) ->
        let rec waitForStart() =
            async {
                let! (message, sender) = inbox.Receive()
                match message with
                | Start ->
                    let source = new CancellationTokenSource()
                    let configuration =
                        { defaultConfig with
                            bindings = [ HttpBinding.create HTTP IPAddress.Loopback port ]
                            cancellationToken = source.Token }
                    let started, service = startWebServerAsync configuration api
                    Async.Start(service,source.Token)
                    let! status = started
                    sender.Reply()
                    return! waitForStop(source)
                | Stop ->
                    sender.Reply()
                    return! waitForStart()
            }
        and waitForStop(source:CancellationTokenSource) =
            async {
                let! (message, sender) = inbox.Receive()
                match message with
                | Stop ->
                    source.Cancel()
                    source.Dispose()
                    sender.Reply()
                    return! waitForStart()
                | Start ->
                    sender.Reply()
                    return! waitForStop(source)
            }
        waitForStart())
    do 
        service.Error.Add(fun ex ->
            let message = ex.Message
            ())

    member x.Start() = service.PostAndReply(fun x -> Start, x)
    member x.Stop() = service.PostAndReply(fun x -> Stop, x)