namespace Nata.IO.EventHub

open Nata.Core
open Nata.IO

type Offset =
    { Partition : Partition 
      Index : Index }

type Offsets = Offset list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partition (x:Offset) = x.Partition
    let index (x:Offset) = x.Index
              
    let (|Offset|_|) =
        String.split '@' >> function
        | [ Integer32 p; Integer64 i ] -> Some { Offset.Partition=p; Index=i }
        | _ -> None

    let toString { Partition=p; Index=i } = sprintf "%d@%d" p i
    let ofString = (|Offset|_|) >> Option.get

    module Codec =
        
        let OffsetToString : Codec<Offset,string> = toString, ofString
        let StringToOffset : Codec<string,Offset> = ofString, toString

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offsets =
    
    let update (xs:Offsets) (x:Offset) : Offsets =
        x :: (xs |> List.filter(Offset.partition >> (<>) x.Partition))

    let merge (offsets:Offsets) =
        Seq.scan (fun (_, os) (e, o) -> Some e, update os o) (None, offsets)
        >> Seq.choose (function Some e, o -> Some(e, o) | _ -> None)

    let partition (partition:Partition) : Offsets -> Offset =
        List.find (Offset.partition >> (=) partition)

    let toString : Offsets -> string =
        List.sortBy Offset.partition 
        >> List.map Offset.toString 
        >> String.concat ","

    let ofString : string -> Offsets =
        String.split ',' 
        >> List.choose Offset.(|Offset|_|) 
        >> List.sortBy Offset.partition

    module Codec =
        
        let OffsetsToString : Codec<Offsets,string> = toString, ofString
        let StringToOffsets : Codec<string,Offsets> = ofString, toString