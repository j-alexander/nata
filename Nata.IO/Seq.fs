namespace Nata.IO

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Seq =

    let mapFst fn = Seq.map <| mapFst fn
    let mapSnd fn = Seq.map <| mapSnd fn

