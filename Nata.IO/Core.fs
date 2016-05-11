namespace Nata.IO

open System

[<AutoOpen>]
module Core =

    let guid _ = Guid.NewGuid().ToString("n")

    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Seq =

        let mapFst fn = Seq.map <| mapFst fn
        let mapSnd fn = Seq.map <| mapSnd fn

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Option =
        
        let whenTrue fn x = if fn x then Some x else None
        let coalesce snd fst = match fst with None -> snd | Some _ -> fst
        let filter fn = Option.bind(whenTrue fn)

        let bindNone fn = function None -> fn() | Some x -> x
        let getValueOr x = function None -> x | Some x -> x
        
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Nullable =
        
        let map (fn : 'T -> 'U) (x : Nullable<'T>) : Nullable<'U> =
            if x.HasValue then Nullable(fn x.Value) else Nullable()
            