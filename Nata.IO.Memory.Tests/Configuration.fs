namespace Nata.IO.Memory.Tests

open System
open Nata.IO.Memory

module Configuration =

    let inline connect() = Stream.connect()
    let inline channel() = Guid.NewGuid().ToString("n")