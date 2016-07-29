namespace Nata.IO.EventHub

open System
open Nata.IO

type PartitionString = string
type Partition = int

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Partition =

    let between (range:Partition*Partition) (x:Partition) =
        Int32.between range x

    let parse : PartitionString -> Partition =
        Int32.Parse

    let ofString : PartitionString -> Partition option =
        Int32.ofString

    let toString : Partition -> PartitionString =
        Int32.toString
        
    module Codec =

        let PartitionToString : Codec<Partition,string> = toString, parse
        let StringToPartition : Codec<string,Partition> = parse, toString