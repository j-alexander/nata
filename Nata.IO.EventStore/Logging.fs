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

        interface ILogger with
            member _.Debug(f: string, args: obj[])                = log.Debug(f, args)
            member _.Debug(ex: Exception, f: string, args: obj[]) = log.Debug(ex, f, args)
            member _.Error(f: string, args: obj[])                = log.Error(f, args)
            member _.Error(ex: Exception, f: string, args: obj[]) = log.Error(ex, f, args)
            member _.Info(f: string, args: obj[])                 = log.Info(f, args)
            member _.Info(ex: Exception, f: string, args: obj[])  = log.Info(ex, f, args)