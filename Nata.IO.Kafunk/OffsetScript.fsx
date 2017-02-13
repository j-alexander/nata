#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk


let watermarksFor (connection:KafkaConn) (topic:string) =
    async {
        let! metadata = Kafka.metadata connection (Metadata.Request [| topic |])
        let topicMetadata = 
            metadata.topicMetadata
            |> Array.tryFind (fun tm -> tm.topicName = topic)
        match topicMetadata with
        | None ->
            return []
        | Some tm ->
            return!
                tm.partitionMetadata
                |> Seq.map (fun pm -> pm.partitionId)
                |> Offsets.offsetRange connection topic
                |> Async.map (Map.toList)
    }

let host = "tcp://guardians-kafka-cluster.qa.jet.com:9092"
let topic = "nova-retailskus-profx2"

let watermarks =
    Kafka.connHostAsync host
    |> Async.bind(fun connection -> watermarksFor connection topic)
    |> Async.RunSynchronously
