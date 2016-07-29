namespace Nata.IO.EventHub
    
open System
open Nata.IO

type IndexString = string
type Index = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Index =

    let start : Index = 0L

    let between (range:Index*Index) (x:Index) =
        Int64.between range x

    let parse : IndexString -> Index =
        Int64.Parse

    let ofString : IndexString -> Index option =
        Int64.ofString

    let toString : Index -> IndexString =
        Int64.toString
        
    module Codec =

        let IndexToString : Codec<Index,string> = toString, parse
        let StringToIndex : Codec<string,Index> = parse, toString