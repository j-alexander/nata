namespace Nata.IO.Memory

open System
open System.Collections.Generic
open System.Collections.Concurrent
open Nata.IO

module ActorQueue =

    type Settings = unit
    type StreamName = string
    type Index = int

    type private StreamEnqueueResult =
        | Success
        | Failure

    type private StreamRequest<'T,'U> =
        | Add of int * BlockingCollection<Event<'T,'U>*Index>
        | Remove of BlockingCollection<Event<'T,'U>*Index>
        | EnqueueAt of Event<'T,'U>*Index*AsyncReplyChannel<StreamEnqueueResult>
        | Enqueue of Event<'T,'U>

    type private IndexRequest<'T,'U> =
        | Locate of StreamName*AsyncReplyChannel<MailboxProcessor<StreamRequest<'T,'U>>>

    let private createStream name = MailboxProcessor.Start <| fun inbox ->
        let events = new List<Event<_,_>>()
        let rec loop listeners = async {
            let! request = inbox.Receive()
            return!
                match request with

                | Add(index, queue) ->
                    for i in [ Math.Max(index,0) .. events.Count-1 ] do
                        queue.Add((events.[i], i))
                    queue :: listeners

                | Remove(queue) ->
                    List.filter ((=) queue) listeners

                | Enqueue(event) ->
                    events.Add(event)
                    for listener in listeners do
                        listener.Add((event, events.Count))
                    listeners

                | EnqueueAt(event,index,sender) ->
                    if (events.Count <> index) then
                        sender.Reply Failure
                    else
                        sender.Reply Success
                        events.Add(event)
                        for listener in listeners do
                            listener.Add((event,index))
                    listeners

                |> loop
        }
        loop []

    let private createIndex () = MailboxProcessor.Start <| fun inbox ->
        let rec loop streams = async {
            let! request = inbox.Receive()
            match request with
            | Locate(name,sender) ->
                match Map.tryFind name streams with
                | Some stream ->
                    sender.Reply(stream)
                    return! loop(streams)
                | None ->
                    let stream = createStream name
                    sender.Reply(stream)
                    return! loop(streams |> Map.add name stream)
        }
        loop Map.empty
            

//        let private locate =
//            ()
//                
//
//
//            fun stream ->
//                ()