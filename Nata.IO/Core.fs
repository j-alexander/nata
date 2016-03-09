namespace Nata.IO

[<AutoOpen>]
module Core =

    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j