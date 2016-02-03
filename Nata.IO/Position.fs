namespace Nata.IO

open System

type Position<'Index> =
    | Start
    | At of 'Index
    | End

type InvalidPosition<'Name,'Index>(stream:'Name,position:Position<'Index>) =
    inherit Exception(sprintf "Invalid Position %A" position)

    member x.Stream = stream
    member x.Position = position