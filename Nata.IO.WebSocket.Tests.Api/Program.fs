namespace Nata.IO.WebSocket.Tests.Api

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

module Program =

    let countText =
        handShake <| fun ws _ ->
            let rec loop(i) =
                socket {
                    let code = Opcode.Text
                    let data =
                        sprintf "%d" i
                        |> Encoding.UTF8.GetBytes
                        |> ByteSegment
                    do! ws.send code data true
                    do!
                      Async.Sleep(1000)
                      |> Async.map Choice1Of2
                    return! loop(i+1)
                }
            loop(0)

    let countBinary =
        handShake <| fun ws _ ->
            let rec loop(i:int32) =
                socket {
                    let code = Opcode.Binary
                    let data =
                        BitConverter.GetBytes(i)
                        |> ByteSegment
                    do! ws.send code data true
                    do!
                      Async.Sleep(1000)
                      |> Async.map Choice1Of2
                    return! loop(i+1)
                }
            loop(0)

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

    [<EntryPoint>]
    let main _ =

        let source = new CancellationTokenSource()
        let configuration =
            { defaultConfig with
                bindings = [ HttpBinding.create HTTP IPAddress.Any 80us ]
                cancellationToken = source.Token }

        startWebServer configuration api
        0