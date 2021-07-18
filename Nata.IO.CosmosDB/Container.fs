namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Linq
open System.Threading.Tasks
open System.Net
open Microsoft.Azure.Cosmos
open Newtonsoft.Json

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

    let internal connect (container:Settings) =

        let properties = new ContainerProperties(container.Name, "/id")

        let client = new CosmosClient(container.Endpoint.Url.AbsoluteUri, container.Endpoint.Key)

        let databaseResponse =
            client.CreateDatabaseIfNotExistsAsync(container.Database)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let database =
            client.GetDatabase(container.Database)

        let documentCollectionResponse =
            database.CreateContainerIfNotExistsAsync(properties)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let container =
            database.GetContainer(container.Name)

        container