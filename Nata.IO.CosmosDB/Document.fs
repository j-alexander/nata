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

    type Id = string
    type Bytes = byte[]

    let connect : Collection -> Source<Id,Bytes,'c> =
        fun collection ->
            let database = new Database(Id=collection.Database)
            let databaseUri = UriFactory.CreateDatabaseUri(collection.Database)
            let documentCollection = new DocumentCollection(Id=collection.Name)
            let documentCollectionUri = UriFactory.CreateDocumentCollectionUri(collection.Database, collection.Name)

            let client = new DocumentClient(collection.Endpoint.Url, collection.Endpoint.Key)

            let databaseResponse =
                client.CreateDatabaseIfNotExistsAsync(database)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let documentCollectionResponse =
                client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            fun (id:Id) ->

                let write ({ Data=bytes } : Event<byte[]>) =
                    let documentResponse =
                        use stream = new MemoryStream(bytes)
                        let document = Resource.LoadFrom<Document>(stream)
                        document.Id <- id
                        client.UpsertDocumentAsync(documentCollectionUri, document)
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                    ()

                let uri = UriFactory.CreateDocumentUri(collection.Database, collection.Name, id)
                let rec read() =
                    seq {
                        let response =
                            client.ReadDocumentAsync(uri)
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
