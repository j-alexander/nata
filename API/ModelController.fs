namespace API

open System
open System.Web.Http
open FSharp.Data

module ModelPath =
    let split (path : string) =
        match path with
        | null -> []
        | text -> text.Split('/') |> Array.toList |> List.filter (String.IsNullOrEmpty >> not)

type ModelController() =
    inherit ApiController()

    static let mutable model = JsonValue.Null

    [<HttpPost; Route("model/{*path}")>]
    member x.Post(path : string, json : JsonValue) =
        model <- model |> Model.update (path |> ModelPath.split) json
        model

    [<HttpGet; Route("model/{*path}")>]
    member x.Get(path : string) =
        model |> Model.read (path |> ModelPath.split)