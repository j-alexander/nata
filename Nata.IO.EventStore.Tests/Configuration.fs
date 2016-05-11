namespace Nata.IO.EventStore.Tests

open System
open Nata.IO
open Nata.IO.EventStore

module Configuration =

    let settings : Settings =
        { Server = { Host = "localhost"
                     Port = 1113 }
          User = { Name = "admin"
                   Password = "changeit" } }
                   
    let channel() = guid()
    let connect() = 
        Stream.connect settings
        |> Source.mapIndex (int64, int)