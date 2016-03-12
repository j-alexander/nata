namespace Nata.IO

open System

type Position<'Index> =
    | Start
    | At of 'Index
    | End

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Position =

    let map (fn:'IndexIn->'IndexOut) = function
        | Position.Start -> Position.Start
        | Position.At index -> index |> fn |> Position.At
        | Position.End -> Position.End
        
type InvalidPosition<'Index>(position:Position<'Index>) =
    inherit Exception(sprintf "Invalid Position %A" position)
    member x.Position = position

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module InvalidPosition =

    let map (fn:'IndexIn->'IndexOut) (exn:InvalidPosition<'IndexIn>) =
        InvalidPosition<'IndexOut>(exn.Position |> Position.map fn)

    let applyMap (fn:'IndexIn->'IndexOut) (f:'In->'Out) (x:'In) : 'Out =
        try f x
        with :? InvalidPosition<'IndexIn> as exn -> raise (map fn exn)
            
