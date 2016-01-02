namespace API

open System
open FSharp.Data


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module List =

    // append a value to the list (necessary for implementing FIFO listeners)
    let append x xs = xs @ [ x ]