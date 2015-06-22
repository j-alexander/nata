namespace API

open System
open FSharp.Data


type Writer = AsyncReplyChannel<Value>
type Write = {
    path : Path
    json : JsonValue
    tick : int64
    writer : Writer option
}
    