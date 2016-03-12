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
    type private Message<'Data,'Metadata> = Sender*Event<'Data,'Metadata>*Position<Index>

    let create (name:'Name) =

        let queue = new BlockingCollection<Event<'Data,'Metadata>>()
        let data = new ConcurrentDictionary<Index, Lazy<Event<'Data,'Metadata>>>()

        let actor = MailboxProcessor<Message<'Data,'Metadata>>.Start <| fun inbox ->
            let rec wait(count) = async {

                let! (sender,event,position) = inbox.Receive()

                let insert() =
                    data.[count+1L] <- lazy(queue.Take())
                    queue.Add(event)
                    let result = data.[count].Force()
                    assert(Object.ReferenceEquals(result, event))
                    sender.Reply(Success count)
                    wait(count+1L)
                let fail() =
                    sender.Reply(Failure)
                    wait(count)

                return!
                    match position with
                    | End ->                        insert()
                    | Start when count=0L ->        insert()
                    | At last when last=count-1L -> insert()
                    | _ ->                          fail()
            }

            data.[0L] <- lazy(queue.Take())
            wait 0L
            
        let writeTo position event= 
            match actor.PostAndReply(fun sender -> sender,event,position) with
            | Success index -> index
            | Failure -> raise (InvalidPosition(position))

        let rec readFrom index =
            seq {
                match data.TryGetValue index with
                | true, event when event.IsValueCreated ->
                    yield event.Value, index
                    yield! readFrom(1L+index)
                | false, _ ->
                    raise (InvalidPosition(Position.At index))
                | _  -> ()
            }

        let rec listenFrom index =
            seq {
                match data.TryGetValue index with
                | true, event ->
                    yield event.Force(), index
                    yield! readFrom(1L+index)
                | _ ->
                    raise (InvalidPosition(Position.At index))
            }

        [   
            Nata.IO.Capability.Reader <| fun () ->
                readFrom 0L |> Seq.map fst

            Nata.IO.Capability.ReaderFrom <| fun index ->
                readFrom (Math.Max(index,0L))

            Nata.IO.Capability.Writer
                (writeTo Position.End >> ignore)

            Nata.IO.Capability.WriterTo <| fun index ->
                (writeTo (Position.At index))

            Nata.IO.Capability.Subscriber <| fun () ->
                listenFrom 0L |> Seq.map fst

            Nata.IO.Capability.SubscriberFrom <| fun index ->
                listenFrom (Math.Max(index,0L))
        ]
           


    let connect : Nata.IO.Connector<Settings,'Name,'Data,'Metadata,Index> =
        
        fun settings ->
            let index = new ConcurrentDictionary<'Name, Nata.IO.Capability<'Data,'Metadata, Index> list>()
            fun name ->
                index.GetOrAdd(name, create)
                