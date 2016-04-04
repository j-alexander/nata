namespace Nata.IO.Memory

open System
open System.Collections.Generic
open System.Collections.Concurrent
open Nata.IO

module Stream =

    type Settings = unit
    type Index = int64
    
    let [<Literal>] Empty = -1

    type private Result = Success of Index | Failure
    type private Sender = AsyncReplyChannel<Result>
    type private Message<'Data> = Sender*Event<'Data>*Position<Index>

    let create (name:'Name) =

        let queue = new BlockingCollection<Event<'Data>>()
        let data = new ConcurrentDictionary<Index, Lazy<Event<'Data>>>()
        
        let rec indexOf = function
            | Position.Start -> 0L
            | Position.Before x -> -1L + indexOf x
            | Position.At x -> Math.Max(0L, x)
            | Position.After x -> 1L + indexOf x
            | Position.End -> int64 (data.Count - 1)

        let actor = MailboxProcessor<Message<'Data>>.Start <| fun inbox ->
            let rec wait(count) = async {

                let! (sender,event,position) = inbox.Receive()

                return!
                    if indexOf position = count then
                        data.[count+1L] <- lazy(queue.Take())
                        queue.Add(event)
                        let result = data.[count].Force()
                        assert(Object.ReferenceEquals(result, event))
                        sender.Reply(Success count)
                        wait(count+1L)
                    else
                        sender.Reply(Failure)
                        wait(count)
            }

            data.[0L] <- lazy(queue.Take())
            wait 0L
            
        let writeTo position event= 
            match actor.PostAndReply(fun sender -> sender,event,position) with
            | Success index -> index
            | Failure -> raise (Position.Invalid(position))

        let rec readFrom index =
            seq {
                match data.TryGetValue index with
                | true, event when event.IsValueCreated ->
                    yield event.Value, index
                    yield! readFrom(1L+index)
                | false, _ ->
                    raise (Position.Invalid(Position.At index))
                | _  -> ()
            }

        let rec listenFrom index =
            seq {
                match data.TryGetValue index with
                | true, event ->
                    yield event.Force(), index
                    yield! readFrom(1L+index)
                | _ ->
                    raise (Position.Invalid(Position.At index))
            }

        [   
            Capability.Reader <| fun () ->
                readFrom 0L |> Seq.map fst

            Capability.ReaderFrom
                (indexOf >> readFrom)

            Capability.Writer
                (writeTo Position.End >> ignore)

            Capability.WriterTo <| fun position ->
                (writeTo position)

            Capability.Subscriber <| fun () ->
                listenFrom 0L |> Seq.map fst

            Capability.SubscriberFrom
                (indexOf >> listenFrom)
        ]
           


    let connect : Connector<Settings,'Name,'Data,Index> =
        
        fun settings ->
            let index = new ConcurrentDictionary<'Name, Capability<'Data, Index> list>()
            fun name ->
                index.GetOrAdd(name, create)
                