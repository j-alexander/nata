namespace Nata.IO.RabbitMQ

open System
open RabbitMQ.Client
open Nata.IO
open System.Text

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

    let subscribe (channel:IModel) (queue:Name) =
        let consumer = new QueueingBasicConsumer(channel)
        channel.BasicConsume(queue, false, consumer) |> ignore
        Seq.initInfinite <| fun i ->
            let message = consumer.Queue.Dequeue()
            let timestamp =
                message.BasicProperties.Timestamp.UnixTime
                |> DateTime.ofUnix
            message.Body
            |> Event.create
            |> Event.withStream queue
            |> Event.withCreatedAt timestamp



    let connect : Connector<HostName,Exchange*Name,byte[],unit> =
        create >> fun (channel:IModel) (exchange:Exchange,name:Name) ->
            let result = declare channel name
            [ 
                Capability.Writer <|
                    write channel exchange name

                Capability.Subscriber <| fun () ->
                    subscribe channel name    
            ]