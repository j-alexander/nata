namespace Nata.IO.AzureStorage

open Nata.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

type Account = CloudStorageAccount

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Account =

    let connection name key =
        sprintf "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s" name key

    let account =
        Account.Parse

    let tryAccount =
        Account.TryParse >> function | true, account -> Some account
                                     | false, _ -> None
         