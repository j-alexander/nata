namespace Nata.IO.Memory.Tests

open System
open Nata.IO
open Nata.IO.Memory

module Configuration =

    let inline connect() = Stream.connect()
    let inline channel() = guid()