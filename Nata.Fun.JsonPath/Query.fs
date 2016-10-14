namespace Nata.Fun.JsonPath

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Threading
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open FSharp.Data

module Query =

    let minimize (json : string) =
        match json with
        | null -> null
        | x when x |> String.IsNullOrWhiteSpace -> x
        | json -> (json |> JsonValue.Parse).ToString(JsonSaveOptions.DisableFormatting)

    let properties (path : string) (json : string) : (string * string) list =
        [   let json = JObject.Parse(json)
            for token in json.SelectTokens(path) do
                for property in token.Children<JProperty>() do
                    yield property.Name, property.Value.ToString()
        ]

    let string (json : JsonValue) =
        json.ToString(JsonSaveOptions.DisableFormatting)

    let strings (path : string) (json : string) : string list =
        [   let json = JObject.Parse(json)
            for token in json.SelectTokens(path) do
                yield token.ToString()
        ]

    let list (json : string) =
        (json |> JsonValue.Parse).AsArray()
        |> Seq.map string
        |> Seq.toList

    let headOr (defaultValue : string) =
        List.ofSeq >> function
                      | x ::  _ -> x
                      | [] -> defaultValue
        
    let first (path : string) (json : string) : string =
        strings path json |> function [] -> "" | x :: _ -> x

    let fields (paths : string list) (json : string) : string list =
        [ for path in paths -> json |> first path ]

    let find (path : string) (json : #seq<string>) : string seq =
        json |> Seq.collect (strings path)

    let findDistinct path = find path >> Seq.distinct
    
    let findFields (paths : string list) (json : #seq<string>) : string list seq =
        seq { for x in json -> [ for path in paths -> x |> first path ] }

    let findFieldsCsv (paths : string list) (json : #seq<string>) : string seq =
        seq {
            for x in json ->
                [ for path in paths -> x |> first path ]
                |> String.concat ","
        }