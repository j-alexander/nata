#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk


let watermarksFor (connection:KafkaConn) (topic:string) =
    async {
        let! metadata = Kafka.metadata connection (Metadata.Request [| topic |])
        match metadata.topicMetadata
              |> Array.tryFind (fun x -> x.topicName = topic) with
        | None ->
            return [||]
        | Some topicMetadata ->
            let request (time:Time) (fn) =
                new OffsetRequest(-1,
                    [| topicMetadata.topicName,
                       [| for p in topicMetadata.partitionMetadata ->
                            p.partitionId, time, MaxNumberOfOffsets.MaxValue |] |])
                |> Kafka.offset connection
                |> Async.map (fun response ->
                    response.topics
                    |> Array.filter (fst >> (=) topic)
                    |> Array.collect (snd >> Array.map (fun po ->
                        po.partition,
                        fn po.offsets))
                    |> Array.sortBy fst)
            let! lows = request Time.EarliestOffset Array.min
            let! highs = request Time.LatestOffset Array.max
            return
                Seq.toArray <|
                    query {
                        for (lp,lo) in lows do
                        join (hp,ho) in highs on (lp = hp)
                        select (topic,lp,lo,ho)
                    }

    }

let host = "tcp://guardians-kafka-cluster.qa.jet.com:9092"
let topic = "nova-retailskus-profx2"

let watermarks =
    Kafka.connHostAsync host
    |> Async.bind(fun connection -> watermarksFor connection topic)
    |> Async.RunSynchronously
