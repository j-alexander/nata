namespace Nata.IO.RabbitMQ

open System
open System.Text
open RabbitMQ.Client

open Nata.Core
open Nata.IO

type HostName = string

module Queue =

    type Exchange = string
    type Name = string

    let create (host:HostName) = 
        let factory = new ConnectionFactory(HostName = host)
        let connection = factory.CreateConnection()
        let channel = connection.CreateModel()
        channel

    let declare (channel:IModel) (queue:Name) =
        channel.QueueDeclare(queue, true, false, false, null)
    
    let write (channel:IModel) (exchange:Exchange) (queue:Name) (event:Event<byte[]>) =
        channel.BasicPublish(exchange, queue, null, event.Data)

    let length (channel:IModel) (queue:Name) =
        let result = declare channel queue
        result.MessageCount
        |> Convert.ToInt64

    let rec index (channel:IModel) (queue:Name) = function
        | Position.Start -> 0L
        | Position.End -> length channel queue
        | Position.At x -> x
        | Position.Before x -> -1L + index channel queue x
        | Position.After x -> 1L + index channel queue x



    let subscribe (channel:IModel) (queue:Name) =
        let consumer = new QueueingBasicConsumer(channel)
        channel.BasicConsume(queue, false, consumer) |> ignore
        Seq.initInfinite <| fun i ->
            let message = consumer.Queue.Dequeue()
            let timestamp =
                message.BasicProperties.Timestamp.UnixTime
                |> DateTime.ofUnixSeconds
            message.Body
            |> Event.create
            |> Event.withStream queue
            |> Event.withCreatedAt timestamp



    let connect : Connector<HostName,Exchange*Name,byte[],int64> =
        create >> fun (channel:IModel) (exchange:Exchange,name:Name) ->
            let result = declare channel name
            [ 
                Capability.Writer <|
                    write channel exchange name

                Capability.Subscriber <| fun () ->
                    subscribe channel name

                Capability.Indexer <|
                    index channel name
            ]