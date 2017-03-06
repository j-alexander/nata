open System
open System.IO

// Framework (Logging & Json)
#r @"bin/Debug/NLog.dll"
#r @"bin/Debug/NLog.FSharp.dll"
#r @"bin/Debug/Newtonsoft.Json.dll"
#r @"bin/Debug/FSharp.Data.dll"
open FSharp.Data

// IO Library & JsonPath
#r @"bin/Debug/Nata.Core.dll"
#r @"bin/Debug/Nata.Fun.JsonPath.dll"
#r @"bin/Debug/Nata.IO.dll"
open Nata
open Nata.Core
open Nata.Fun.JsonPath
open Nata.IO
open Nata.IO.Capability

// Shared Memory Stream:
#r @"bin/Debug/Nata.IO.Memory.dll"
open Nata.IO.Memory

// Disk File:
#r @"bin/Debug/Nata.IO.File.dll"
open Nata.IO.File

// EventStore:
#r @"bin/Debug/EventStore.ClientAPI.dll"
#r @"bin/Debug/Nata.IO.EventStore.dll"
open Nata.IO.EventStore

// Kafka:
#r @"bin/Debug/kafka-net.dll"
#r @"bin/Debug/Nata.IO.Kafka.dll"
open Nata.IO.Kafka

// RabbitMQ:
#r @"bin/Debug/RabbitMQ.Client.dll"
#r @"bin/Debug/Nata.IO.RabbitMQ.dll"
open Nata.IO.RabbitMQ

// Azure Storage:
#r @"bin/Debug/Microsoft.WindowsAzure.Storage.dll"
#r @"bin/Debug/Nata.IO.AzureStorage.dll"
open Nata.IO.AzureStorage

// Web Sockets:
#r @"bin/Debug/WebSocket4Net.dll"
#r @"bin/Debug/Nata.IO.WebSocket.dll"
open Nata.IO.WebSocket

let settings =
    { Settings.defaultSettings with
        Server=
            { Server.localhost with
                Host="10.107.0.69" } }

let source =
    Stream.connect Settings.defaultSettings //settings
    |> Source.mapData JsonValue.Codec.BytesToJsonValue

let target =
    Stream.connect Settings.defaultSettings
    |> Source.mapData JsonValue.Codec.BytesToJsonValue

let indexOf, readerFrom =
    let stream =
        source "skus"
    stream
    |> Capability.indexer,
    stream
    |> Capability.readerFrom

let writer =
    let stream =
        target "skus"
    stream
    |> Capability.writer
    
let endIndex = indexOf Position.End
let startIndex = indexOf Position.Start

let fromStartIndex =
    readerFrom (Position.Before (Position.At endIndex))
    |> Seq.toList