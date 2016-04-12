namespace Nata.IO.AzureStorage

open System
open System.IO
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

        let read (container:CloudBlobContainer) (blobName:string) =
            let reference = container.GetBlockBlobReference(blobName)
            Seq.initInfinite <| fun i ->
                use memory = new MemoryStream()
                reference.DownloadToStream(memory)

                let bytes = memory.ToArray()
                let withLastModified =
                    match reference.Properties.LastModified with
                    | x when x.HasValue -> Event.withCreatedAt x.Value.UtcDateTime
                    | x -> id

                Event.create bytes
                |> Event.withName blobName
                |> Event.withEventType reference.Properties.ContentType
                |> withLastModified

        let readFrom (container:CloudBlobContainer) (blobName:string) =
            let reference = container.GetBlockBlobReference(blobName)
            let satisfies = function
                | Position.At etag ->
                    (=) etag
                | Position.Before _ | Position.After _ | Position.Start | Position.End ->
                    fun _ -> true
            Seq.unfold <| fun (position:Position<string>) ->
                use memory = new MemoryStream()
                reference.DownloadToStream(memory)

                let bytes = memory.ToArray()
                let withLastModified =
                    match reference.Properties.LastModified with
                    | x when x.HasValue -> Event.withCreatedAt x.Value.UtcDateTime
                    | x -> id
                let etag = reference.Properties.ETag

                Event.create bytes
                |> Event.withName blobName
                |> Event.withEventType reference.Properties.ContentType
                |> withLastModified
                |> function | x when satisfies position etag -> Some((x, etag), position)
                            | _ -> None