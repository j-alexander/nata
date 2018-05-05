namespace Nata.IO.Kafunk

open System
open Kafunk

type Connection = {
    Cluster : KafkaConn
    Settings : Settings
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Connection =

    let cluster { Cluster=x } = x
    let settings { Settings=x } = x

    let create (settings:Settings) =
        { Connection.Settings = settings
          Connection.Cluster =
            let hosts =
              settings.Hosts
              |> List.map KafkaUri.parse
            KafkaConfig.create(hosts,version=Versions.V_0_9_0)
            |> Kafka.conn }
