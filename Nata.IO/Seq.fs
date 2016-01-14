namespace Nata.IO

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seq =

    let mapFst fn = Seq.map (fun (i,j) -> fn i, j)
    let mapSnd fn = Seq.map (fun (i,j) -> i, fn j)

