namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Linq
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client
open Newtonsoft.Json

open Nata.IO

module Document =

    type Version = string

    let connect : Collection -> Source<Id,Bytes,Version> =
        fun collection ->

            let client, uri = Collection.connect collection

            fun (id:Id) ->
                let documentUri = UriFactory.CreateDocumentUri(collection.Database, collection.Name, id)

                let writeTo : WriterTo<Bytes,Version> =
                    function
                    | Position.At null
                    | Position.Start ->
                        fun { Data=bytes } ->
                            let documentResponse =
                                use stream = new MemoryStream(bytes)
                                let document = Resource.LoadFrom<Document>(stream)
                                document.Id <- id
                                client.CreateDocumentAsync(uri, document, disableAutomaticIdGeneration=true)
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                            documentResponse.Resource.ETag
                    | Position.At etag ->
                        let condition = new AccessCondition(Condition=etag, Type=AccessConditionType.IfMatch)
                        let options = new RequestOptions(AccessCondition=condition)
                        fun { Data=bytes } ->
                            let documentResponse =
                                use stream = new MemoryStream(bytes)
                                let document = Resource.LoadFrom<Document>(stream)
                                document.Id <- id
                                client.ReplaceDocumentAsync(uri, document, options)
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                            documentResponse.Resource.ETag
                    | Position.End ->
                        fun ({ Data=bytes } : Event<Bytes>) ->
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
                        let response =
                            client.ReadDocumentAsync(documentUri)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                        let document = response.Resource
                        yield
                            document.ToByteArray()
                            |> Event.create
                            |> Event.withCreatedAt document.Timestamp
                            |> Event.withName document.Id
                            |> Event.withTag document.ETag,
                            document.ETag
                        yield! readFrom position
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
