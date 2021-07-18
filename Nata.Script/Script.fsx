open System
open System.IO

#r "netstandard"

// Framework (Logging & Json)
#r @"bin/Debug/net50/NLog.dll"
#r @"bin/Debug/net50/Newtonsoft.Json.dll"
#r @"../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open FSharp.Data

// IO Library & JsonPath
#r @"bin/Debug/net50/Nata.Core.dll"
#r @"bin/Debug/net50/Nata.Fun.JsonPath.dll"
#r @"bin/Debug/net50/Nata.IO.dll"
open Nata
open Nata.Core
open Nata.Fun.JsonPath
open Nata.IO
open Nata.IO.Capability

// Service Capabilities
#r @"bin/Debug/net50/Nata.Service.dll"
open Nata.Service

// Shared Memory Stream:
#r @"bin/Debug/net50/Nata.IO.Memory.dll"
open Nata.IO.Memory

// Disk File:
#r @"bin/Debug/net50/Nata.IO.File.dll"
open Nata.IO.File

// EventStore:
#r @"bin/Debug/net50/protobuf-net.dll"
#r @"bin/Debug/net50/EventStore.ClientAPI.dll"
#r @"bin/Debug/net50/Nata.IO.EventStore.dll"
open Nata.IO.EventStore

// KafkaNet:
#r @"bin/Debug/net50/kafka-net-for-dotnet-core.dll"
#r @"bin/Debug/net50/Nata.IO.KafkaNet.dll"
open Nata.IO.KafkaNet

// Kafunk:
#r @"bin/Debug/net50/FSharp.Control.AsyncSeq.dll"
#r @"bin/Debug/net50/kafunk.dll"
#r @"bin/Debug/net50/Nata.IO.Kafunk.dll"
open Nata.IO.KafkaNet

// RabbitMQ:
#r @"bin/Debug/net50/RabbitMQ.Client.dll"
#r @"bin/Debug/net50/Nata.IO.RabbitMQ.dll"
open Nata.IO.RabbitMQ

// Azure Storage:
#r @"bin/Debug/net50/Microsoft.WindowsAzure.Storage.dll"
#r @"bin/Debug/net50/Nata.IO.AzureStorage.dll"
open Nata.IO.AzureStorage

// Web Sockets:
#r @"bin/Debug/net50/WebSocket4Net.dll"
#r @"bin/Debug/net50/Nata.IO.WebSocket.dll"
open Nata.IO.WebSocket

// Consul:
#r @"bin/Debug/net50/Consul.dll"
#r @"bin/Debug/net50/Nata.IO.Consul.dll"
open Nata.IO.Consul

// CosmosDB:
#r @"bin/Debug/net50/Microsoft.Azure.Cosmos.Client.dll"
#r @"bin/Debug/net50/Nata.IO.CosmosDB.dll"
open Nata.IO.CosmosDB
