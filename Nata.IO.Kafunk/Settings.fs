namespace Nata.IO.Kafka

open System
open Kafunk

type Settings = {
    Hosts : string list
    FetchMaxWaitTime : TimeSpan
    FetchMinBytes : MinBytes
    FetchMaxBytes : MaxBytes
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Settings =

    let hosts { Hosts=x } = x
    let fetchMaxWaitTime { FetchMaxWaitTime=x } = x
    let fetchMinBytes { FetchMinBytes=x } = x
    let fetchMaxBytes { FetchMaxBytes=x } = x

    let defaultSettings =
        { Hosts = [ "localhost" ]
          FetchMaxWaitTime = TimeSpan.FromMilliseconds 500.
          FetchMinBytes = 65536
          FetchMaxBytes = 4194304 }

    let connect (settings:Settings) =
        settings.Hosts
        |> List.map KafkaUri.parse
        |> KafkaConfig.create
        |> Kafka.conn,
        settings
