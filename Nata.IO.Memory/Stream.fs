namespace Nata.IO.Memory

open System
open System.Collections.Generic
open System.Collections.Concurrent
open Nata.IO

module Stream =

    type Settings = unit
    type Index = int
    
    let [<Literal>] Empty = -1

    type Position =
        | Start
        | At of Index
        | Any

    type InvalidPosition<'Name>(stream:'Name,position:Position) =
        inherit Exception(sprintf "Invalid Position %A" position)
        member x.Stream = stream
        member x.Position = position

    type private Result = Success of Index | Failure
    type private Sender = AsyncReplyChannel<Result>
    type private Message<'Data,'Metadata> = Sender*Event<'Data,'Metadata>*Position

    let create (name:'Name) =

        let queue = new BlockingCollection<Event<'Data,'Metadata>>()
        let data = new ConcurrentDictionary<Index, Lazy<Event<'Data,'Metadata>>>()

        let actor = MailboxProcessor<Message<'Data,'Metadata>>.Start <| fun inbox ->
            let rec wait(count) = async {

                let! (sender,event,position) = inbox.Receive()

                let insert() =
                    data.[count+1] <- lazy(queue.Take())
                    queue.Add(event)
                    let result = data.[count].Force()
                    assert(Object.ReferenceEquals(result, event))
                    sender.Reply(Success count)
                    wait(count+1)
                let fail() =
                    sender.Reply(Failure)
                    wait(count)

                return!
                    match position with
                    | Any ->                       insert()
                    | Start when count=0 ->        insert()
                    | At last when last=count-1 -> insert()
                    | _ ->                         fail()
            }

            data.[0] <- lazy(queue.Take())
            wait 0
            
        let writeTo position event= 
            match actor.PostAndReply(fun sender -> sender,event,position) with
            | Success index -> index
            | Failure -> raise (InvalidPosition(name,position))

        let rec readFrom index =
            seq {
                match data.TryGetValue index with
                | true, event when event.IsValueCreated ->
                    yield event.Value, index
                    yield! readFrom(1+index)
                | false, _ ->
                    raise (InvalidPosition(name,Position.At index))
                | _  -> ()
            }

        let rec listenFrom index =
            seq {
                match data.TryGetValue index with
                | true, event ->
                    yield event.Force(), index
                    yield! readFrom(1+index)
                | _ ->
                    raise (InvalidPosition(name,Position.At index))
            }

        [   
            Nata.IO.Capability.Reader <| fun () ->
                readFrom 0 |> Seq.map fst

            Nata.IO.Capability.ReaderFrom <| fun index ->
                readFrom (Math.Max(index,0))

            Nata.IO.Capability.Writer
                (writeTo Position.Any >> ignore)

            Nata.IO.Capability.WriterTo <| fun index ->
                (writeTo (Position.At index))

            Nata.IO.Capability.Subscriber <| fun () ->
                listenFrom 0 |> Seq.map fst

            Nata.IO.Capability.SubscriberFrom <| fun index ->
                listenFrom (Math.Max(index,0))
        ]
           


    let connect : Nata.IO.Connector<Settings,'Name,'Data,'Metadata,Index> =
        
        fun settings ->
            let index = new ConcurrentDictionary<'Name, Nata.IO.Capability<'Data,'Metadata, Index> list>()
            fun name ->
                index.GetOrAdd(name, create)
                