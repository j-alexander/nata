namespace API

open System
open FSharp.Data


type Value = {
    json : JsonValue
    tick : int64
}


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Value =

    // create a new value with the specified json and tick
    let create json tick =
        { json = json
          tick = tick }

    // create an empty value
    let empty = create JsonValue.Null 0L
