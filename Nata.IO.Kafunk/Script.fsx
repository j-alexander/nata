#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk

let host = "tcp://guardians-kafka-cluster.qa.jet.com:9092"
let topic = "nova-retailskus-profx2"

let connection =
    Kafka.connHost host

let metadata =
    Kafka.metadata connection (Metadata.Request [| topic |])
    |> Async.RunSynchronously

let brokers : Broker[] = metadata.brokers
(*
[| {host = "10.107.0.175"; nodeId = 1; port = 9092;}
   {host = "10.107.0.176"; nodeId = 2; port = 9092;}
   {host = "10.107.0.179"; nodeId = 3; port = 9092;}
   {host = "10.107.0.180"; nodeId = 4; port = 9092;} |]
*)

let topicMetadata : TopicMetadata[] = metadata.topicMetadata
(*
[| topicErrorCode = 0s;
   topicName = "nova-retailskus-profx2";
   partitionMetadata = [| {isr = [|2; 4; 3|]; leader = 2; partitionErrorCode = 0s; partitionId = 23; replicas = [|2; 3; 4|];}
                          {isr = [|4; 3; 1|]; leader = 4; partitionErrorCode = 0s; partitionId = 17; replicas = [|4; 3; 1|];}
                          ... |] |]
*)


let leaderOffsetRequests (time:Time, max:MaxNumberOfOffsets) =
    topicMetadata
    |> Array.collect (fun t ->
        t.partitionMetadata
        |> Array.map (fun p ->
            p.leader,
            t.topicName,
            p.partitionId))
    |> Array.groupBy (fun (l,_,_) -> l)
    |> Array.map (fun (l,ltp) ->
        let tp =
            ltp
            |> Array.groupBy (fun (_,t,_) -> t)
            |> Array.map (fun (topic,ltp) ->
                topic,
                ltp
                |> Array.map (fun (_,_,partitionId) ->
                    partitionId,
                    time,
                    max))
        new OffsetRequest(l,tp))

let leaderOffsetResponses =
    leaderOffsetRequests (Time.EarliestOffset, MaxNumberOfOffsets.MaxValue)
    |> Array.map (Kafka.offset connection)
    |> Async.Parallel
    |> Async.RunSynchronously


let offsetRequest (time:Time, max:MaxNumberOfOffsets) =
    topicMetadata
    |> Array.map (fun t ->
        t.topicName,
        t.partitionMetadata
        |> Array.map (fun p ->
            p.partitionId,
            time,
            max))
    |> fun ts -> new OffsetRequest(-1, ts)
let offsetResponse (time:Time) =
    offsetRequest (time, MaxNumberOfOffsets.MaxValue)
    |> Kafka.offset connection
    |> Async.RunSynchronously
    
let watermarks =
    let watermarksFor =
        offsetResponse
        >>
        fun response ->
            response.topics
            |> Array.collect (fun (t, pos) ->
                pos
                |> Array.map (fun po ->
                    t,
                    po.partition,
                    po.offsets))
            |> Array.sortBy (fun (t,p,os) ->
                p)
    let low =
        watermarksFor Time.EarliestOffset
        |> Array.map (fun (t,p,os) ->
            t,
            p,
            Array.min(os))
        |> Array.groupBy (fun (t,p,o) -> t)
    let high =
        watermarksFor Time.LatestOffset
        |> Array.map (fun (t,p,os) ->
            t,
            p,
            Array.max(os))
        |> Array.groupBy (fun (t,p,o) -> t)
    let joinByTopic =
        query {
            for (lt,ls) in low do
            join (ht,hs) in high on (lt = ht)
            select (lt,ls,hs)
        }
    [|
        for (t,ls,hs) in joinByTopic ->
            query {
                for (lt,lp,lo) in ls do
                join (ht,hp,ho) in hs on (lp = hp)
                select (t,lp,lo,ho)
            }
            |> Seq.toArray
    |]

//let offsetFetchResponse =
//    Kafka.offsetFetch connection (new OffsetFetchRequest()
//    |> Async.RunSynchronously

//let config = ConsumerConfig.create(null, topic)
//let consumer = Consumer.create connection config
//
//Consumer.consume consumer <| fun (state) (messages) ->
//    async {
//        printfn "member_id=%s topic=%s partition=%i" state.memberId messages.topic messages.partition
//    }
//|> Async.RunSynchronously
//
//Consumer.s