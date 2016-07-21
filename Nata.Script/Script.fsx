open System
open System.IO

#r @"C:\Users\Jonathan\Source\nata\packages\FSharp.Data\lib\net40\FSharp.Data.dll"
open FSharp.Data

#r @"bin/Debug/NLog.dll"
#r @"bin/Debug/NLog.FSharp.dll"
#r @"bin/Debug/Newtonsoft.Json.dll"
#r @"bin/Debug/EventStore.ClientAPI.dll"
#r @"bin/Debug/kafka-net.dll"
#r @"bin/Debug/Nata.Fun.JsonPath.dll"
#r @"bin/Debug/Nata.IO.dll"
#r @"bin/Debug/Nata.IO.EventStore.dll"
#r @"bin/Debug/Nata.IO.Kafka.dll"

open Nata
open Nata.IO
open Nata.IO.EventStore
open Nata.IO.Kafka
open Nata.IO.Capability
open Nata.Fun.JsonPath
