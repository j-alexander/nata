namespace Nata.IO.EventHub
    
open System
open Nata.IO

type Index = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Index =

    let start : Index = 0L

    let between (range:Index*Index) (x:Index) =
        Int64.between range x

    let parse : string -> Index =
        Int64.Parse

    let ofString : string -> Index option =
        Int64.ofString

    let toString : Index -> string =
        Int64.toString