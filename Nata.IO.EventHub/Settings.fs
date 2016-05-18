namespace Nata.IO.EventHub

open System

type Settings = {
    Connection : string
    MaximumWaitTimeOnRead : TimeSpan
}