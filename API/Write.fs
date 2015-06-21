namespace API

open System
open FSharp.Data


type Write = {
    path : Path
    json : JsonValue
    tick : int64
}
    