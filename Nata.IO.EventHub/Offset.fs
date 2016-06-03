namespace Nata.IO.EventHub

type Offset =
    { Partition : Partition 
      Index : Index }

type Offsets = Offset list

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partition (x:Offset) = x.Partition
    let index (x:Offset) = x.Index

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offsets =
    
    let update (xs:Offsets) (x:Offset) : Offsets =
        x :: (xs |> List.filter(Offset.partition >> (<>) x.Partition))

    let merge (offsets:Offsets) =
        Seq.scan (fun (_, os) (e, o) -> Some e, update os o) (None, offsets)
        >> Seq.choose (function Some e, o -> Some(e, o) | _ -> None)