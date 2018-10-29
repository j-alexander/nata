module Nata.Core.NLog

open System.Diagnostics
open NLog

module Logger =

    let getWithName (name:string) =
        LogManager.GetLogger(name)

    let get () =
        LogManager.GetLogger(
            StackTrace(1, false)
                .GetFrames().[0]
                .GetMethod()
                .DeclaringType
                .Name)

    let internal err (e:exn) m = (e,m)

    let trace (log:Logger) f =
        Printf.kprintf (log.Trace) f

    let traceException (log:Logger) e f =
        Printf.kprintf (err e >> log.Trace) f

    let debug (log:Logger) f =
        Printf.kprintf (log.Debug) f

    let debugException (log:Logger) e f =
        Printf.kprintf (err e >> log.Debug) f

    let info (log:Logger) f =
        Printf.kprintf (log.Info) f

    let infoException (log:Logger) e f =
        Printf.kprintf (err e >> log.Info) f

    let warn (log:Logger) f =
        Printf.kprintf (log.Warn) f

    let warnException (log:Logger) e f =
        Printf.kprintf (err e >> log.Warn) f

    let error (log:Logger) f =
        Printf.kprintf (log.Error) f

    let errorException (log:Logger) e f =
        Printf.kprintf (err e >> log.Error) f

    let fatal (log:Logger) f =
        Printf.kprintf (log.Fatal) f

    let fatalException (log:Logger) e f =
        Printf.kprintf (err e >> log.Fatal) f

type Logger with

    member log.trace f =
        Logger.trace log f

    member log.traceException e f =
        Logger.traceException log e f

    member log.debug f =
        Logger.debug log f

    member log.debugException e f =
        Logger.debugException log e f

    member log.info f =
        Logger.info log f

    member log.infoException e f =
        Logger.infoException log e f

    member log.warn f =
        Logger.warn log f

    member log.warnException e f =
        Logger.warnException log e f

    member log.error f =
        Logger.error log f

    member log.errorException e f =
        Logger.errorException log e f

    member log.fatal f =
        Logger.fatal log f

    member log.fatalException e f =
        Logger.fatalException log e f
