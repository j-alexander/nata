namespace API

open System
open FSharp.Data


type Nullifier = AsyncReplyChannel<Listen list>
type Nullify = {
    nullifier : Nullifier
}
