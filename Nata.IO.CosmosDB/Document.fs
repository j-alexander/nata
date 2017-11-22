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

    let connect : Collection -> Source<Id,Bytes,'c> =
        fun collection ->

            let client, uri = Collection.connect collection

            fun (id:Id) ->
                let documentUri = UriFactory.CreateDocumentUri(collection.Database, collection.Name, id)

                let write ({ Data=bytes } : Event<byte[]>) =
                    let documentResponse =
                        use stream = new MemoryStream(bytes)
                        let document = Resource.LoadFrom<Document>(stream)
                        document.Id <- id
                        client.UpsertDocumentAsync(uri, document, disableAutomaticIdGeneration=true)
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                    ()

                let rec read() =
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
                            |> Event.withTag document.ETag
                        yield! read()
                    }

                [
                    Nata.IO.Writer <| write

                    Nata.IO.Reader <| read
                ]
