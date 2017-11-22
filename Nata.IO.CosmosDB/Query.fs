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

open Nata.IO

type Query = string
type ContinuationToken = string

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Query =

    let connect : Collection -> Source<Query,Bytes,ContinuationToken> =
        fun collection ->

            let client, uri = Collection.connect collection

            fun query ->

                let read() =
                    client.CreateDocumentQuery<Document>(uri, query)
                    |> Seq.map (fun document ->
                        document.ToByteArray()
                        |> Event.create
                        |> Event.withCreatedAt document.Timestamp
                        |> Event.withName document.Id
                        |> Event.withTag document.ETag)

                let rec readFrom(token) =
                    seq {
                        let options =
                            match token with
                            | Position.At token when not (String.IsNullOrEmpty token) ->
                                new FeedOptions(
                                    MaxItemCount = Nullable(1),
                                    RequestContinuation = token)
                            | Position.At _
                            | Position.Start ->
                                new FeedOptions(
                                    MaxItemCount = Nullable(1))
                            | position ->
                                raise (new NotSupportedException(sprintf "Position %A is not supported by a Query." position))
                        let query =
                            client.CreateDocumentQuery<Document>(uri, query, options).AsDocumentQuery()

                        let rec iterate token =
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
                                    yield! iterate response.ResponseContinuation
                            }
                        yield! iterate null
                    }

                [
                    Nata.IO.Reader <| read
                    Nata.IO.ReaderFrom <| readFrom
                ]