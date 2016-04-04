namespace Nata.IO

open System

type Position<'Index> =
    | Start
    | Before of Position<'Index>
    | At of 'Index
    | After of Position<'Index>
    | End

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Position =


    let rec map (fn:'IndexIn->'IndexOut) = function
        | Position.Start -> Position.Start
        | Position.Before position -> position |> map fn |> Position.Before
        | Position.At index -> index |> fn |> Position.At
        | Position.After position -> position |> map fn |> Position.After
        | Position.End -> Position.End
        
    
    type Invalid<'Index>(position:Position<'Index>) =
        inherit Exception(sprintf "Invalid Position %A" position)
        member x.Position = position

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Invalid =
        let map (fn:'IndexIn->'IndexOut) (exn:Invalid<'IndexIn>) =
            Invalid<'IndexOut>(exn.Position |> map fn)
        
    let applyMap (fn:'IndexIn->'IndexOut) (f:'In->'Out) (x:'In) : 'Out =
        try f x
        with :? Invalid<'IndexIn> as exn ->
            raise (Invalid.map fn exn)
        