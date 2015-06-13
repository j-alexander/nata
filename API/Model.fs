namespace API

open System
open FSharp.Data

module Model =

    let rec update (path : string list) (value : JsonValue) (model : JsonValue) =
        match path with
        | [] -> value
        | head :: tail ->
            match model with
            | JsonValue.Record record ->
                let map = Map.ofSeq record
                let model = match map |> Map.tryFind head with None -> JsonValue.Null | Some x -> x

                map
                |> Map.add head (model |> update tail value)
                |> Map.toArray
                |> JsonValue.Record

            | JsonValue.Array xs ->
                match Int32.TryParse head with
                | true, i when i < xs.Length && i >= 0 ->
                    let xs = xs |> Array.copy
                    xs.[i] <- (xs.[i] |> update tail value)
                    xs |> JsonValue.Array

                | _ ->
                    [| head, (JsonValue.Null |> update tail value) |] |> JsonValue.Record
            | _ ->
                [|  head, (JsonValue.Null |> update tail value) |] |> JsonValue.Record

    let rec read (path : string list) (model : JsonValue) =
        match path with
        | [] -> model
        | head :: tail ->
            match model with
            | JsonValue.Record xs ->
                match xs |> Map.ofSeq |> Map.tryFind head with
                | Some model -> model |> read tail
                | None -> JsonValue.Null
            | JsonValue.Array xs ->
                match Int32.TryParse head with
                | true, i when i < xs.Length && i >= 0 -> xs.[i] |> read tail
                | _ -> JsonValue.Null 
            | _ -> JsonValue.Null