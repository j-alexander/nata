namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Linq
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client
open Newtonsoft.Json

open Nata.Core
open Nata.IO

module Document =

    type Version = string

    let (|ResourceNotFound|_|) : exn -> unit option=
        function
        | :? DocumentClientException as e
        | AggregateException (:? DocumentClientException as e)
            when e.StatusCode = Nullable(HttpStatusCode.NotFound) -> Some()
        | _ -> None

    let (|ResourceAlreadyExists|_|) : exn -> unit option=
        function
        | :? DocumentClientException as e
        | AggregateException (:? DocumentClientException as e)
            when e.StatusCode = Nullable(HttpStatusCode.Conflict) -> Some()
        | _ -> None

    let (|PreconditionNotMet|_|) : exn -> unit option=
        function
        | :? DocumentClientException as e
        | AggregateException (:? DocumentClientException as e)
            when e.StatusCode = Nullable(HttpStatusCode.PreconditionFailed) -> Some()
        | _ -> None

    let connect : Collection -> Source<Id,Bytes,Version> =
        fun collection ->

            let client, uri = Collection.connect collection

            fun (id:Id) ->
                let documentUri = UriFactory.CreateDocumentUri(collection.Database, collection.Name, id)

                let writeTo : WriterTo<Bytes,Version> =
                    fun position { Data=bytes } ->
                        match position with
                        | Position.After (Position.At null)
                        | Position.At null
                        | Position.Start ->
                            use stream = new MemoryStream(bytes)
                            let document = Resource.LoadFrom<Document>(stream)
                            document.Id <- id
                            client.CreateDocumentAsync(uri, document, disableAutomaticIdGeneration=true)
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response -> response.Resource.ETag
                            | Choice2Of2 ResourceAlreadyExists -> raise (new Position.Invalid<_>(position))
                            | Choice2Of2 e -> Async.reraise(e)
                        | Position.After (Position.At etag)
                        | Position.At etag ->
                            let condition = new AccessCondition(Condition=etag, Type=AccessConditionType.IfMatch)
                            let options = new RequestOptions(AccessCondition=condition)
                            use stream = new MemoryStream(bytes)
                            let document = Resource.LoadFrom<Document>(stream)
                            document.Id <- id
                            client.ReplaceDocumentAsync(documentUri, document, options)
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response -> response.Resource.ETag
                            | Choice2Of2 ResourceNotFound
                            | Choice2Of2 PreconditionNotMet -> raise (new Position.Invalid<_>(position))
                            | Choice2Of2 e -> Async.reraise(e)
                        | Position.End ->
                            let documentResponse =
                                use stream = new MemoryStream(bytes)
                                let document = Resource.LoadFrom<Document>(stream)
                                document.Id <- id
                                client.UpsertDocumentAsync(uri, document, disableAutomaticIdGeneration=true)
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                            documentResponse.Resource.ETag
                        | Position.Before _ ->
                            raise (new NotSupportedException("Position.Before is not supported."))
                        | Position.After _ ->
                            raise (new NotSupportedException("Position.After is not supported."))

                let write =
                    writeTo Position.End
                    >> ignore

                let rec readFrom position =
                    seq {
                        yield!
                            client.ReadDocumentAsync(documentUri)
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response ->
                                let document = response.Resource
                                seq {
                                    yield
                                        document.ToByteArray()
                                        |> Event.create
                                        |> Event.withCreatedAt document.Timestamp
                                        |> Event.withName document.Id
                                        |> Event.withTag document.ETag,
                                        document.ETag
                                    yield! readFrom position
                                }
                            | Choice2Of2 ResourceNotFound -> Seq.empty
                            | Choice2Of2 e -> Async.reraise(e)
                    }

                let read() =
                    readFrom Position.End
                    |> Seq.map fst

                [
                    Nata.IO.Writer <| write

                    Nata.IO.WriterTo <| writeTo

                    Nata.IO.Reader <| read

                    Nata.IO.ReaderFrom <| readFrom
                ]
