namespace API

open System
open System.Diagnostics
open System.Threading.Tasks
open System.Web.Http.ExceptionHandling

module Log =

    let tracer =
        { new IExceptionLogger with
            override x.LogAsync(context, token) : Task =
                new Action(fun () ->
                    Trace.TraceInformation("{0} - request: {1}", context.Exception, context.Request))
                |> Task.Run
        }