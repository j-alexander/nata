#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk

let fetch connection topic partition offset =

    let request =
        let replicaId : ReplicaId = -1
        let maxWaitTime : MaxWaitTime = 500
        let minBytes : MinBytes = 65536
        let maxBytes : MaxBytes = 1048576
        new FetchRequest(
            replicaId,
            maxWaitTime,
            minBytes,
            [|
                topic,
                [|
                    partition,
                    offset,
                    maxBytes
                |]
            |])

    let response =
        Kafka.fetch connection request
        |> Async.RunSynchronously

    match response.topics with
    | [| t, [| p, ec, hwmo, mss, ms |] |] ->
        match ec with
        | ErrorCode.NoError ->                 Some(ConsumerMessageSet(t, p, ms, hwmo))
        | ErrorCode.OffsetOutOfRange ->        None
        | ErrorCode.NotLeaderForPartition ->   failwith "Not leader for partition."
        | ErrorCode.UnknownTopicOrPartition -> failwith "Unknown topic or partition."
        | ErrorCode.ReplicaNotAvailable ->     failwith "Replica not available."
        | x -> failwith <| sprintf "Unrecognized fetch error code: %A" x
    | x ->     failwith <| sprintf "Unrecognized fetch response: %A" x

let consume live connection topic partition =
    Seq.unfold(fun offset ->
        match fetch connection topic partition offset with
        | None -> None
        | Some cms ->
            match cms.messageSet.messages with
            | [||] when not live -> None
            | [||] ->
                Some(None, offset)
            | messages ->
                let offset =
                    messages
                    |> Seq.map (fun (o,s,m) -> o)
                    |> Seq.max
                    |> (+) 1L
                Some(Some cms,offset))
    >> Seq.choose id
    >> Seq.collect (fun cms ->
        cms.messageSet.messages
        |> Seq.map (fun (offset,size,message) ->
            cms.topic,
            cms.partition,
            offset,
            size,
            message))

let read live connection topic partition =
    consume false live connection topic partition

let listen live connection topic partition =
    consume true live connection topic partition
         
        
let host = "tcp://localhost:9092"
let topic = "topic"

let connection = Kafka.connHost host
let partition = 7
let offset = 21485330L

let messages =
    read connection topic partition offset
    |> Seq.toList
    
listen connection topic partition offset
|> Seq.iter (printfn "%A")

