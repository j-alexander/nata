namespace Nata.IO.EventHub

open System
open Nata.IO

type Partition = int

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Partition =

    let between (range:Partition*Partition) (x:Partition) =
        Int32.between range x

    let parse : string -> Partition =
        Int32.Parse

    let ofString : string -> Partition option =
        Int32.ofString

    let toString : Partition -> string =
        Int32.toString