namespace API

open System
open System.IO
open System.Text
open System.Net.Http.Formatting
open System.Net.Http.Headers
open FSharp.Data

module Format =

    module Parser =

        let private (|Value|_|) (text:string) : JsonValue option =
            try text |> JsonValue.Parse |> Some
            with _ -> None

        let private (|Null|_|) (text:string) : JsonValue option =
            match text with
            | null | "null" -> JsonValue.Null |> Some
            | text when String.IsNullOrWhiteSpace(text) -> JsonValue.Null |> Some
            | _ -> None

        let private (|Boolean|_|) (text:string) : JsonValue option =
            match bool.TryParse text with
            | true, value -> value |> JsonValue.Boolean |> Some
            | _ -> None

        let private (|Number|_|) (text:string) : JsonValue option =
            match Decimal.TryParse text with
            | true, value -> value |> JsonValue.Number |> Some
            | _ -> None

        let private (|Float|_|) (text:string) : JsonValue option =
            match Double.TryParse text with
            | true, value -> value |> JsonValue.Float |> Some
            | _ -> None

        let private (|String|_|) (text:string) : JsonValue option =
            match text with
            | null -> None
            | text ->
                if (text.Length > 1 &&
                    text.StartsWith("\"") &&
                    text.EndsWith("\"")) then
                    text.Substring(1, text.Length - 2) |> JsonValue.String |> Some
                else
                    None

        let parse = function
            | Null x -> Some x
            | Value x -> Some x
            | Boolean x -> Some x
            | Number x -> Some x
            | Float x -> Some x
            | String x -> Some x
            | _ -> None

    module Encoder =
        
        let rec write = function
            | JsonValue.Null -> "null"
            | JsonValue.String x -> x |> sprintf "\"%s\""
            | JsonValue.Boolean x -> x.ToString()
            | JsonValue.Float x -> x.ToString()
            | JsonValue.Number x -> x.ToString()
            | JsonValue.Array xs ->
                xs
                |> Seq.map write
                |> String.concat ", "
                |> sprintf "[ %s ]"
            | JsonValue.Record xs ->
                xs
                |> Seq.map (fun (k,v) -> v |> write |> sprintf "\"%s\": %s" k)
                |> String.concat ", " 
                |> sprintf "{ %s }"

    let plain =
        let instance = {
            new MediaTypeFormatter() with
                override x.CanReadType(t) = true
                override x.CanWriteType(t) = true
                override x.WriteToStreamAsync(t, value, stream, content, transport) =
                    let text =
                        match value with
                        | :? JsonValue as json -> json |> Encoder.write
                        | :? string as text -> text
                        | x -> x.ToString()
                    let writer = new StreamWriter(stream, Encoding.UTF8)
                    writer.WriteLine(text)
                    writer.FlushAsync()
                override x.ReadFromStreamAsync(t, readStream, stream, content, transport) =
                    async {
                        let reader = new StreamReader(readStream)
                        let! text = Async.AwaitTask(reader.ReadToEndAsync())
                        return
                            if typeof<JsonValue> = t then
                                match text |> Parser.parse with
                                | Some json -> json :> obj
                                | None ->
                                    text
                                    |> sprintf "A valid json value is required here: \"%s\""
                                    |> failwith
                            else
                                text :> obj
                    } |> Async.StartAsTask
        }
        instance.SupportedMediaTypes.Clear()
        instance.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"))
        instance.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"))
        instance
