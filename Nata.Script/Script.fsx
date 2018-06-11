open System
open System.IO

// Framework (Logging & Json)
#r @"bin/Debug/NLog.dll"
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
#r @"bin/Debug/protobuf-net.dll"
#r @"bin/Debug/EventStore.ClientAPI.NetCore.dll"
#r @"bin/Debug/Nata.IO.EventStore.dll"
open Nata.IO.EventStore

// KafkaNet:
#r @"bin/Debug/kafka-net.dll"
#r @"bin/Debug/Nata.IO.KafkaNet.dll"
open Nata.IO.KafkaNet

// Kafunk:
#r @"bin/Debug/FSharp.Control.AsyncSeq.dll"
#r @"bin/Debug/kafunk.dll"
#r @"bin/Debug/Nata.IO.Kafunk.dll"
open Nata.IO.KafkaNet

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

// Consul:
#r @"bin/Debug/Consul.dll"
#r @"bin/Debug/Nata.IO.Consul.dll"
open Nata.IO.Consul

// CosmosDB:
#r @"bin/Debug/Microsoft.Azure.DocumentDB.Core.dll"
#r @"bin/Debug/Nata.IO.CosmosDB.dll"
open Nata.IO.CosmosDB
