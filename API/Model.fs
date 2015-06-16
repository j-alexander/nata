namespace API

open System
open FSharp.Data

module Model =

    /// write a value to the model at the specified path
    let rec update (path : string list) (value : JsonValue) (model : JsonValue) =
        match path with
        // @ the destination node, return the new value as the residual sub-model
        | [] -> value
        // @ an intermediate node:
        | head :: tail ->
            match model with
            // if the structure is a record
            | JsonValue.Record record ->
                // find the sub-model under json property name = head
                let map = Map.ofSeq record
                let model =
                    match map |> Map.tryFind head with
                    // that sub-model exists
                    | Some x -> x
                    // or does not exist
                    | None -> JsonValue.Null 

                map
                // update the sub-model, and place it at property name = head
                |> Map.add head (model |> update tail value)
                |> Map.toArray
                // integrate the value into the residual sub-model
                |> JsonValue.Record

            // for an array:
            | JsonValue.Array xs ->
                // read the index position into the array
                match Int32.TryParse head with
                | true, i when i < xs.Length && i >= 0 ->
                    // retain an immutable instance of the array
                    let xs = xs |> Array.copy
                    // update the sub-model at index position
                    xs.[i] <- (xs.[i] |> update tail value)
                    // return the updated array as the sub-model for this node
                    xs |> JsonValue.Array

                | _ ->
                    // index is a key, not a position, replace the array:
                    [| head, (JsonValue.Null |> update tail value) |] |> JsonValue.Record
            | _ ->
                // current position has no subtree -> create a new sub-model
                [|  head, (JsonValue.Null |> update tail value) |] |> JsonValue.Record

    /// read the sub-model from model at the specified path
    let rec read (path : string list) (model : JsonValue) =
        match path with
        // @ the destination node, return the residual sub-model
        | [] -> model
        // @ an intermediate node:
        | head :: tail ->
            match model with
            // if the structure is a record:
            | JsonValue.Record xs ->
                match xs |> Map.ofSeq |> Map.tryFind head with
                | Some model -> model |> read tail
                | None -> JsonValue.Null
            // for an array, read the index position in the array
            | JsonValue.Array xs ->
                match Int32.TryParse head with
                // if successful, read into the index position of the array:
                | true, i when i < xs.Length && i >= 0 -> xs.[i] |> read tail
                // otherwise, the position is invalid == return null
                | _ -> JsonValue.Null 
            // the current position has no subtree == return null
            | _ -> JsonValue.Null