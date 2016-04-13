namespace Nata.IO.AzureStorage

open Nata.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

type Account = CloudStorageAccount

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Account =

    let connection(name, key) =
        sprintf "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s" name key

    let create =
        Account.Parse

    let tryCreate =
        Account.TryParse >> function | true, account -> Some account
                                     | false, _ -> None
         
    let createFrom =
        connection >> create

    let tryCreateFrom =
        connection >> tryCreate
        