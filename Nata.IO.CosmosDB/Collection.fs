namespace Nata.IO.CosmosDB

open System

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
          Collection.Name = collection}