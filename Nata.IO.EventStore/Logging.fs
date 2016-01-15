namespace Nata.EventStore

open System
open System.Diagnostics
open System.Net
open System.Net.Sockets
open NLog.FSharp
open EventStore.ClientAPI
open EventStore.ClientAPI.SystemData

module Logging =

    type EventStoreNLogLogger(log:Logger) =

        let format f a = String.Format(f,a)

        interface ILogger with
            member x.Debug(f,a) =   log.Debug            "%s" (format f a)
            member x.Debug(e,f,a) = log.DebugException e "%s" (format f a)
            member x.Error(f,a) =   log.Error            "%s" (format f a)
            member x.Error(e,f,a) = log.ErrorException e "%s" (format f a)
            member x.Info(f,a) =    log.Info             "%s" (format f a)
            member x.Info(e,f,a) =  log.InfoException e  "%s" (format f a)