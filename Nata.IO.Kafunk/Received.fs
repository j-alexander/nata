namespace Nata.IO.Kafka

open System
open System.Net
open System.Text
open NLog.FSharp
open Kafunk

type Received =
    { Topic : Topic
      Partition : Partition
      Offset : Index
      Size : int
      BatchOf : int
      Key : Data
      Value : Data
      Timestamp : DateTime option }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Received =
        
    let topic { Topic=x } = x
    let partition { Partition=x } = x
    let offset { Offset=x } = x
    let size { Size=x } = x
    let batchOf { BatchOf=x } = x
    let key { Key=x } = x
    let value { Value=x } = x
    let timestamp { Timestamp=x } = x