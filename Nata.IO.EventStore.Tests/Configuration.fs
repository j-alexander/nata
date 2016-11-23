namespace Nata.IO.EventStore.Tests

open System
open Nata.Core
open Nata.IO
open Nata.IO.EventStore

module Configuration =

    let settings : Settings = Settings.defaultSettings
                   
    let channel() = guid()
    let connect() = 
        Stream.connect settings
        |> Source.mapIndex (int64, int)