namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Linq
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client
open Microsoft.Azure.Documents.Linq
open Newtonsoft.Json

open Nata.Core
open Nata.IO

type Query = string
and Parameters = Map<Key,Value>
and Key = string
and Value = obj

type ContinuationToken = string

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Query =

    let connectWithParameters : Collection -> Source<Query*Parameters,Bytes,ContinuationToken> =
        fun collection ->

            let client, uri = Collection.connect collection

            fun (query, parameters) ->
                let sqlQuerySpec =
                    let parameters =
                        parameters
                        |> Map.toSeq
                        |> Seq.map (fun (k,v) -> new SqlParameter(k,v))
                    new SqlQuerySpec(QueryText=query,
                                     Parameters=new SqlParameterCollection(parameters))

                let read() =
                    client.CreateDocumentQuery<Document>(uri, sqlQuerySpec)
                    |> Seq.map (fun document ->
                        document.ToByteArray()
                        |> Event.create
                        |> Event.withCreatedAt document.Timestamp
                        |> Event.withName document.Id
                        |> Event.withTag document.ETag)

                let readFrom =
                    let rec iterate (query:IDocumentQuery<Document>) token =
                        seq {
                            if query.HasMoreResults then
                                let response =
                                    query.ExecuteNextAsync<Document>()
                                    |> Async.AwaitTask
                                    |> Async.RunSynchronously
                                yield!
                                    response
                                    |> Seq.map (fun document ->
                                        document.ToByteArray()
                                        |> Event.create
                                        |> Event.withCreatedAt document.Timestamp
                                        |> Event.withName document.Id
                                        |> Event.withTag document.ETag,
                                        token)
                                yield! iterate query response.ResponseContinuation
                        }

                    let execute token options =
                        seq {
                            let query = client.CreateDocumentQuery<Document>(uri, sqlQuerySpec, options).AsDocumentQuery()
                            yield! iterate query token
                        }

                    let rec start skip =
                        function
                        | Position.After x -> start (skip+1) x
                        | Position.Before x -> start (skip-1) x
                        | Position.At null
                        | Position.Start ->
                            if skip <= 0 then
                                execute null (new FeedOptions(MaxItemCount = Nullable(1)))
                            else
                                execute null (new FeedOptions(MaxItemCount = Nullable(1)))
                                |> Seq.trySkip skip
                        | Position.At token when not (String.IsNullOrEmpty token) ->
                            if skip < 0 then
                                raise (new NotSupportedException(sprintf "Position cannot rewind by %d" (Math.Abs skip)))
                            elif skip = 0 then
                                execute null (new FeedOptions(MaxItemCount = Nullable(1), RequestContinuation = token))
                            else
                                execute null (new FeedOptions(MaxItemCount = Nullable(1), RequestContinuation = token))
                                |> Seq.trySkip skip
                        | Position.End when skip >= 0 -> Seq.empty
                        | position ->
                            raise (new NotSupportedException(sprintf "Position %A is not supported." position))

                    start 0

                [
                    Nata.IO.Reader <| read
                    Nata.IO.ReaderFrom <| readFrom
                ]

    let connect : Collection -> Source<Query,Bytes,ContinuationToken> =
        let queryWithoutParameters =
            (fun (q,p) -> q),
            (fun (q) -> q, Map.empty)
        connectWithParameters
        >> Source.mapChannel queryWithoutParameters