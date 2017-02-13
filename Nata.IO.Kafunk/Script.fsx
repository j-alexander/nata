#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk

let host = "tcp://guardians-kafka-cluster.qa.jet.com:9092"
let topic = "nova-retailskus-profx2"

let connection =
    Kafka.connHost host


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