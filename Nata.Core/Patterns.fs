namespace Nata.Core

open System

[<AutoOpen>]
module Patterns =

    let (|Nullable|_|) = Nullable.toOption
    let (|Integer64|_|) = Int64.ofString
    let (|Integer32|_|) = Int32.ofString
    let (|DateTime|_|) = DateTime.ofString