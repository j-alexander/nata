namespace API

open System
open System.Web.Http
open FSharp.Data


type ModelController() =
    inherit ApiController()

    static let mutable model = JsonValue.Null

    [<HttpPost; Route("model/{*path}")>]
    member x.Post(path : string, json : JsonValue) =
        model <- model |> Model.update (path |> Path.split) json
        model

    [<HttpGet; Route("model/{*path}")>]
    member x.Get(path : string) =
        model |> Model.read (path |> Path.split)