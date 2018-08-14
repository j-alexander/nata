open System
open System.IO

// Framework (Logging & Json)
#r @"bin/Debug/net461/NLog.dll"
#r @"bin/Debug/net461/Newtonsoft.Json.dll"
#r @"../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open FSharp.Data

// IO Library & JsonPath
#r @"bin/Debug/net461/Nata.Core.dll"
#r @"bin/Debug/net461/Nata.Fun.JsonPath.dll"
#r @"bin/Debug/net461/Nata.IO.dll"
open Nata
open Nata.Core
open Nata.Fun.JsonPath
open Nata.IO
open Nata.IO.Capability

// Shared Memory Stream:
#r @"bin/Debug/net461/Nata.IO.Memory.dll"
open Nata.IO.Memory

// Disk File:
#r @"bin/Debug/net461/Nata.IO.File.dll"
open Nata.IO.File

// EventStore:
#r @"bin/Debug/net461/protobuf-net.dll"
#r @"bin/Debug/net461/EventStore.ClientAPI.NetCore.dll"
#r @"bin/Debug/net461/Nata.IO.EventStore.dll"
open Nata.IO.EventStore

// KafkaNet:
#r @"bin/Debug/net461/kafka-net.dll"
#r @"bin/Debug/net461/Nata.IO.KafkaNet.dll"
open Nata.IO.KafkaNet

// Kafunk:
#r @"bin/Debug/net461/FSharp.Control.AsyncSeq.dll"
#r @"bin/Debug/net461/kafunk.dll"
#r @"bin/Debug/net461/Nata.IO.Kafunk.dll"
open Nata.IO.KafkaNet

// RabbitMQ:
#r @"bin/Debug/net461/RabbitMQ.Client.dll"
#r @"bin/Debug/net461/Nata.IO.RabbitMQ.dll"
open Nata.IO.RabbitMQ

// Azure Storage:
#r @"bin/Debug/net461/Microsoft.WindowsAzure.Storage.dll"
#r @"bin/Debug/net461/Nata.IO.AzureStorage.dll"
open Nata.IO.AzureStorage

// Web Sockets:
#r @"bin/Debug/net461/WebSocket4Net.dll"
#r @"bin/Debug/net461/Nata.IO.WebSocket.dll"
open Nata.IO.WebSocket

// Consul:
#r @"bin/Debug/net461/Consul.dll"
#r @"bin/Debug/net461/Nata.IO.Consul.dll"
open Nata.IO.Consul

// CosmosDB:
#r @"bin/Debug/net461/Microsoft.Azure.DocumentDB.Core.dll"
#r @"bin/Debug/net461/Nata.IO.CosmosDB.dll"
open Nata.IO.CosmosDB
