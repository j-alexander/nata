namespace API

open Owin
open System
open System.Web.Http
open System.Web.Http.ExceptionHandling
open Microsoft.Owin.Hosting
open Microsoft.Owin.Diagnostics

module Service =

    type Startup() =
        member x.Configuration(app : IAppBuilder) =

            let configuration = new HttpConfiguration()
            configuration.MapHttpAttributeRoutes()
            configuration.Formatters.Clear()
            configuration.Formatters.Add(Format.plain)
            configuration.Services.Add(typeof<IExceptionLogger>, Log.tracer)
            configuration.EnsureInitialized()

            app.UseWebApi(configuration) |> ignore
            //app.UseErrorPage() |> ignore

    let defaultEndpoint = "http://localhost:8887"

    let start (endpoint:string) : IDisposable = WebApp.Start<Startup>(endpoint)