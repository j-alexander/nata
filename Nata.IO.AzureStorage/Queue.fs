namespace Nata.IO.AzureStorage

open System
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Queue
open Nata.IO

type Queue = CloudQueue

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Queue =

    type Name = string

    let create (account:Account) =
        let client = account.CreateCloudQueueClient()
        fun (name:Name) ->
            let queue = client.GetQueueReference(name)
            let result = queue.CreateIfNotExists()
            queue

    let write (queue:Queue) (event:Event<byte[]>) =
        let message = new CloudQueueMessage(event.Data)
        queue.AddMessage(message)

    let subscribe (queue:Queue) =
        Seq.initInfinite <| fun i ->
            let message = queue.GetMessage()
            let created =
                message.InsertionTime
                |> Nullable.map DateTime.ofOffset
            Event.create message.AsBytes
            |> Event.withStream queue.Name
            |> Event.withCreatedAtNullable created