namespace Nata.IO.EventHub

open System
open Nata.IO

type PartitionId = string
type Partition = int

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Partition =

    let between (range:Partition*Partition) (x:Partition) =
        Int32.between range x

    let parse : PartitionId -> Partition =
        Int32.Parse

    let ofString : PartitionId -> Partition option =
        Int32.ofString

    let toString : Partition -> PartitionId =
        Int32.toString