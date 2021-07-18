namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Text
open System.Linq
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Cosmos
open Microsoft.Azure.Cosmos.Linq
open Newtonsoft.Json
open FSharp.Data

open Nata.Core
open Nata.IO

type Query = string
and Parameters = Map<Key,Value>
and Key = string
and Value = obj

type ContinuationToken = string

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Query =
    
    let private toDocuments (bytes:byte[]) =
        let json =
            Encoding.UTF8.GetString bytes
            |> JsonValue.Parse
            |> JsonValue.tryGet "Documents"
        match json with
        | Some (JsonValue.Array xs) -> Array.map JsonValue.toBytes xs
        | _ -> raise (new InvalidDataException("Cosmos query response is missing the 'Documents' element."))
    
    let private toEvents (documents:seq<byte[]>) =
        seq {
            for bytes in documents do
                let data, metadata =
                    bytes
                    |> Document.Metadata.get
                yield
                    data
                    |> Event.create
                    |> Event.withName metadata.id
                    |> Event.withCreatedAt metadata._ts
                    |> Event.withTag metadata._etag
        }
    
    let connectWithParameters : Container.Settings -> Source<Query*Parameters,Bytes,ContinuationToken> =
        fun container ->

            let container = Container.connect container

            fun (query, parameters) ->
                let queryDefinition =
                    parameters
                    |> Map.toSeq
                    |> Seq.fold (fun (acc:QueryDefinition) (k,v) ->
                        acc.WithParameter(k,v)) (new QueryDefinition(query))

                let rec iterate (batchSize:Nullable<int>) token =
                    seq {
                        use iterator =
                            let options = new QueryRequestOptions(MaxItemCount = batchSize)
                            container.GetItemQueryStreamIterator(queryDefinition, token, options)
                        let rec loop token =
                            seq {
                                if iterator.HasMoreResults then
                                    let response =
                                        iterator.ReadNextAsync()
                                        |> Async.AwaitTask
                                        |> Async.RunSynchronously               
                                    let events =
                                        use stream = new MemoryStream()
                                        response.Content.CopyTo(stream)
                                        stream.ToArray()
                                        |> toDocuments
                                        |> toEvents
                                    for event in events do
                                        yield event, token
                                    yield! loop response.ContinuationToken
                            }
                        yield! loop token
                    }
                
                let read() =
                    iterate (Nullable()) null
                    |> Seq.map fst

                let readFrom =

                    let rec start skip =
                        function
                        | Position.After x -> start (skip+1) x
                        | Position.Before x -> start (skip-1) x
                        | Position.At _ when skip < 0 ->
                            raise (new NotSupportedException(sprintf "Position cannot rewind by %d" (Math.Abs skip)))
                        | Position.At null
                        | Position.Start ->
                            iterate (Nullable(1)) null
                            |> Seq.trySkip skip
                        | Position.At token when not (String.IsNullOrEmpty token) ->
                            iterate (Nullable(1)) token
                            |> Seq.trySkip skip
                        | Position.End when skip >= 0 -> Seq.empty
                        | position ->
                            raise (new NotSupportedException(sprintf "Position %A is not supported." position))

                    start 0

                [
                    Nata.IO.Reader <| read
                    Nata.IO.ReaderFrom <| readFrom
                ]

    let connect : Container.Settings -> Source<Query,Bytes,ContinuationToken> =
        let queryWithoutParameters =
            (fun (q,p) -> q),
            (fun (q) -> q, Map.empty)
        connectWithParameters
        >> Source.mapChannel queryWithoutParameters