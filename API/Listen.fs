namespace API

open System
open FSharp.Data


type Listener = MailboxProcessor<Value>
type Listen = {
    path : Path
    listener : Listener
}
