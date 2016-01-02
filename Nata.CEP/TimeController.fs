namespace API

open System
open System.Web.Http
open FSharp.Data

type TimeController() =
    inherit ApiController()

    [<HttpGet; Route("time")>]
    member x.Time() = 
        [| "time", DateTime.UtcNow.ToString("o") |> JsonValue.String |] 
        |> JsonValue.Record