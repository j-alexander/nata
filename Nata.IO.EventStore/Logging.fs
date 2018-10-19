namespace Nata.IO.EventStore

open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open NLog
open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData

module Logging =

    type EventStoreNLogLogger(log:Logger) =

        let format f a = String.Format(f,a)

        interface ILogger with
            member x.Debug(f,a)   = log.Debug(format f a)
            member x.Debug(e,f,a) = log.Debug(e,format f a)
            member x.Error(f,a)   = log.Error(format f a)
            member x.Error(e,f,a) = log.Error(e,format f a)
            member x.Info(f,a)    = log.Info(format f a)
            member x.Info(e,f,a)  = log.Info(e,format f a)