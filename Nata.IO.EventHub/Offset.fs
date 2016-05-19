namespace Nata.IO.EventHub

type Offset =
    { Partition : Partition 
      Index : Index }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Offset =

    let partition (x:Offset) = x.Partition
    let index (x:Offset) = x.Index