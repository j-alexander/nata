namespace Nata.IO.Consul

open System
open System.Text
open Consul
open Nata.IO

type Client = IKVEndpoint
type Key = string
type Value = byte[]

type WriteException(s) =
    inherit Exception(s)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Client =

    let create { Settings.Address=address
                 Settings.DataCenter=dataCenter } =
        let client = new ConsulClient(fun configuration ->
            configuration.Address <- new Uri(address)
            configuration.Datacenter <- dataCenter)
        client.KV
    
    let toEvent : KVPair -> Event<Key*Value> option =
        function
        | null -> None
        | pair ->
            (pair.Key, pair.Value)
            |> Event.create
            |> Some

    let ofEvent : Event<Key*Value> -> KVPair =
        Event.data >> fun (key,value) ->
            let pair = new KVPair(key)
            pair.Value <- value
            pair
        
    let list (client:Client) prefix = 
        let result =
            client.List(prefix)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        result.Response
        |> Seq.choose toEvent

    let read (client:Client) key =
        let result=
            client.Get(key)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        result.Response
        |> toEvent

    let write (client:Client) event =
        let result =
            client.Put(ofEvent event)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        if not result.Response then
            let message =
                sprintf "Write failed with status %A after %A"
                <| result.StatusCode
                <| result.RequestTime
            raise(new WriteException(message))
        

    let connect (settings:Settings) =
        let client = create settings
        fun prefix ->
            [
                Nata.IO.Capability.Reader <| fun () ->
                    list client prefix

                Nata.IO.Capability.Writer <|
                    write client
            ]