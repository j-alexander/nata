namespace Nata.IO.CosmosDB

open System

type Endpoint = {
    Url : Uri
    Key : string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Endpoint =

    let url { Endpoint.Url=x } = x
    let key { Endpoint.Key=x } = x
    
    let emulator =
        { Endpoint.Url = new Uri("https://localhost:8081")
          Endpoint.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" }