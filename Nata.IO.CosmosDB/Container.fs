namespace Nata.IO.CosmosDB

open Nata.Core.Patterns
open Microsoft.Azure.Cosmos

type Id = string
type Bytes = byte[]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Container =
    
    type Settings = {
        Endpoint : Endpoint
        Database : string
        Name : string
    }

    let endpoint { Settings.Endpoint=x } = x
    let database { Settings.Database=x } = x
    let name  { Settings.Name=x } = x
    
    let create endpoint database container =
        { Settings.Endpoint = endpoint
          Settings.Database = database
          Settings.Name = container }

    let internal connect { Endpoint={ Url=url
                                      Key=key } as endpoint
                           Database=database
                           Name=name} =

        let properties = new ContainerProperties(name, "/id")
        let client =
            let options = new CosmosClientOptions()
            options.ConnectionMode <- ConnectionMode.Gateway
            if (Endpoint.emulator = endpoint) then
                new CosmosClient($"AccountEndpoint={url.AbsoluteUri};AccountKey={key};DisableServerCertificateValidation=True", options)
            else
                new CosmosClient(url.AbsoluteUri, key)

        let databaseResponse =
            client.CreateDatabaseIfNotExistsAsync(database)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let database =
            client.GetDatabase(database)

        let documentCollectionResponse =
            database.CreateContainerIfNotExistsAsync(properties)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let container =
            database.GetContainer(name)

        container