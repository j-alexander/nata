namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open KafkaNet
open KafkaNet.Model
open KafkaNet.Protocol

module Partition =
    
    let x = 3