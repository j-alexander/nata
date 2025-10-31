namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Linq
open System.Net.Http
open System.Net.Security
open System.Security.Cryptography.X509Certificates
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Documents
open Microsoft.Azure.Documents.Client
open Newtonsoft.Json

type Id = string
type Bytes = byte[]

type Collection = {
    Endpoint : Endpoint
    Database : string
    Name : string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Collection =

    let endpoint { Collection.Endpoint=x } = x
    let database { Collection.Database=x } = x
    let name  { Collection.Name=x } = x
    
    let create endpoint database collection =
        { Collection.Endpoint = endpoint
          Collection.Database = database
          Collection.Name = collection }

    let internal connect (collection:Collection) =

        let database = new Database(Id=collection.Database)
        let databaseUri = UriFactory.CreateDatabaseUri(collection.Database)
        let documentCollection = new DocumentCollection(Id=collection.Name)
        let documentCollectionUri = UriFactory.CreateDocumentCollectionUri(collection.Database, collection.Name)

        let httpClientHandler : HttpClientHandler =
            if Endpoint.emulator = collection.Endpoint then
                let handler = new HttpClientHandler()
                let callback (message:HttpRequestMessage)
                             (cert:X509Certificate)
                             (chain:X509Chain)
                             (errors:SslPolicyErrors) =
                    true
                handler.ServerCertificateCustomValidationCallback <- new Func<_,_,_,_,_>(callback)
                handler
            else null

        let client = new DocumentClient(collection.Endpoint.Url, collection.Endpoint.Key, httpClientHandler)

        let databaseResponse =
            client.CreateDatabaseIfNotExistsAsync(database)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let documentCollectionResponse =
            client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        client, documentCollectionUri