namespace Nata.IO.AzureStorage

open System
open System.IO
open System.Net
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob
open Nata.IO

module Blob =

    let container name (account:Account) =
        let client = account.CreateCloudBlobClient()
        let container = client.GetContainerReference(name)
        let result = container.CreateIfNotExists()
        container

    module Block =
    
        let write (container:CloudBlobContainer) (blobName:string) =
            let reference = container.GetBlockBlobReference(blobName)
            fun (event:Event<byte[]>) ->
                reference.UploadFromByteArray(event.Data, 0, event.Data.Length)

        let writeTo (container:CloudBlobContainer) (blobName:string) =
            let reference = container.GetBlockBlobReference(blobName)
            fun (position:Position<string>) ->
                let condition = function
                    | Position.Start -> AccessCondition.GenerateIfNotExistsCondition()
                    | Position.Before x -> AccessCondition.GenerateEmptyCondition()
                    | Position.At etag -> AccessCondition.GenerateIfMatchCondition(etag)
                    | Position.After x -> AccessCondition.GenerateEmptyCondition()
                    | Position.End -> AccessCondition.GenerateEmptyCondition()
                fun (event:Event<byte[]>) ->
                    reference.UploadFromByteArray(event.Data, 0, event.Data.Length, condition position)
                    reference.Properties.ETag

        let readFrom (container:CloudBlobContainer) (blobName:string) =
            let reference = container.GetBlockBlobReference(blobName)
            let condition = function
                | Position.Start -> AccessCondition.GenerateIfNotExistsCondition()
                | Position.Before x -> AccessCondition.GenerateEmptyCondition()
                | Position.At etag -> AccessCondition.GenerateIfMatchCondition(etag)
                | Position.After x -> AccessCondition.GenerateEmptyCondition()
                | Position.End -> AccessCondition.GenerateEmptyCondition()
            let generator =
                Seq.unfold <| fun (position:Position<string>) ->
                    try 
                        use memory = new MemoryStream()
                        reference.DownloadToStream(memory, condition position)
                    
                        let bytes = memory.ToArray()
                        let withLastModified =
                            match reference.Properties.LastModified with
                            | x when x.HasValue -> Event.withCreatedAt x.Value.UtcDateTime
                            | x -> id
                        let etag, contentType =
                            reference.Properties.ETag,
                            reference.Properties.ContentType
                        let event =
                            Event.create bytes
                            |> Event.withName blobName
                            |> Event.withTag etag
                            |> Event.withEventType contentType
                            |> withLastModified
                        Some ((event, etag), position)
                    with
                    | :? StorageException as e when e.RequestInformation.HttpStatusCode = 412 ->
                        None
            fun position ->
                seq {
                    if reference.Exists() then yield! generator position
                }

        let read container blobName =
            readFrom container blobName Position.End |> Seq.map fst