namespace Nata.IO

[<AutoOpen>]
module Core =

    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Seq =

        let mapFst fn = Seq.map <| mapFst fn
        let mapSnd fn = Seq.map <| mapSnd fn

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Option =
        
        let whenTrue fn x = if fn x then Some x else None
        let bindNone fn = function None -> fn() | Some x -> x
        let coalesce snd fst = match fst with None -> snd | Some _ -> fst
        let filter fn = Option.bind(whenTrue fn)

            
            