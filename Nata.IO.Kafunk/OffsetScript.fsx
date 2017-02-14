#r "bin/Debug/FSharp.Control.AsyncSeq.dll"
#r "bin/Debug/Kafunk.dll"

open System
open Kafunk

let host = "tcp://localhost"
let topic = "topic"
let group = "group"

let connection = Kafka.connHost host

let watermarks =
    Offsets.offsetRange connection topic Array.empty
    |> Async.map Map.toList
    |> Async.RunSynchronously

let progress =
    ConsumerInfo.progress connection group topic Array.empty
    |> Async.RunSynchronously



