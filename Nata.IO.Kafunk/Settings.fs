namespace Nata.IO.Kafunk

open System
open Kafunk

type Settings = {
    Hosts : string list
    ClientId : string
    FetchMaxWaitTime : TimeSpan
    FetchMinBytes : MinBytes
    FetchMaxBytes : MaxBytes
    PreallocateProducer : bool
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Settings =

    let hosts { Hosts=x } = x
    let clientId { ClientId=x } = x
    let fetchMaxWaitTime { FetchMaxWaitTime=x } = x
    let fetchMinBytes { FetchMinBytes=x } = x
    let fetchMaxBytes { FetchMaxBytes=x } = x
    let preallocateProducer { PreallocateProducer=x } = x

    let defaultSettings =
        { Hosts = [ "localhost" ]
          ClientId = String.Empty
          FetchMaxWaitTime = TimeSpan.FromMilliseconds 500.
          FetchMinBytes = 65536
          FetchMaxBytes = 4194304 
          PreallocateProducer = false }
