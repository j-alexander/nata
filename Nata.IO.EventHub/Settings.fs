namespace Nata.IO.EventHub

open System

type Settings = {
    Connection : string
    MaximumMessageCountOnRead : int
    MaximumWaitTimeOnRead : TimeSpan
}