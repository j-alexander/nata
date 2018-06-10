namespace Nata.IO.AzureStorage

open System
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Queue
open Nata.Core
open Nata.IO

type Queue = CloudQueue

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Queue =

    type Name = string

    let create (account:Account) =
        let client = account.CreateCloudQueueClient()
        fun (name:Name) ->
            let queue = client.GetQueueReference(name)
            queue.CreateIfNotExistsAsync()
            |> Task.wait
            queue

    let write (queue:Queue) (event:Event<byte[]>) =
        let message = new CloudQueueMessage(null)
        message.SetMessageContent(event.Data)
        queue.AddMessageAsync(message)
        |> Task.wait

    let subscribe (queue:Queue) =
        Seq.initInfinite <| fun i ->
            let message =
                queue.GetMessageAsync()
                |> Task.waitForResult
            let created =
                message.InsertionTime
                |> Nullable.map DateTime.ofOffset
            Event.create message.AsBytes
            |> Event.withStream queue.Name
            |> Event.withIndex (int64 i)
            |> Event.withCreatedAtNullable created

    let length (queue:Queue) =
        queue.FetchAttributesAsync()
        |> Task.wait
        queue.ApproximateMessageCount
        |> Nullable.toOption
        |> Option.map Convert.ToInt64

    let rec index (queue:Queue) = function
        | Position.Start -> 0L
        | Position.End -> length queue |> Option.defaultValue 0L
        | Position.At x -> x
        | Position.Before x -> -1L + index queue x
        | Position.After x -> 1L + index queue x



    let connect : Connector<Account,Name,byte[],int64> =
        fun account -> create account >> fun queue ->
                [ 
                    Capability.Writer <|
                        write queue

                    Capability.Subscriber <| fun () ->
                        subscribe queue

                    Capability.Indexer <|
                        index queue
                ]