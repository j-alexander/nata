namespace Nata.Service

open System
open Nata.Core
open Nata.IO

module Hub =

    module Snapshot =

        type private Listener<'Data,'Index> =
            AsyncReplyChannel<Event<'Data>*'Index>

        type private Listeners<'Data,'Index> =
            Listener<'Data,'Index> list

        type private Message<'Data,'Index> =
            | Publish of Event<'Data>*'Index
            | Subscribe of Listener<'Data,'Index>
            | Next of Listener<'Data,'Index>*'Index

        let create (subscribeFrom : SubscriberFrom<'Data,'Index>) =

            let hub = MailboxProcessor<Message<'Data,'Index>>.Start <| fun inbox ->

                Async.Start <| async {
                    subscribeFrom(Position.Before Position.End)
                    |> Seq.iter(Publish >> inbox.Post)
                }

                let rec loop(snapshot, index, listeners:Listeners<'Data,'Index>) =
                    async {
                        let! message = inbox.Receive()
                        match message with
                        | Publish (snapshot, index) ->
                            for listener in listeners do
                                listener.Reply(snapshot, index)
                            return! loop(snapshot, index, [])
                        | Next (listener, last) when last=index ->
                            return! loop(snapshot, index, listener :: listeners)
                        | Next (listener, _)
                        | Subscribe (listener) ->
                            do listener.Reply(snapshot, index)
                            return! loop(snapshot, index, listeners)
                    }

                let rec start(listeners:Listeners<'Data,'Index>) =
                    async {
                        let! message = inbox.Receive()
                        match message with
                        | Publish (snapshot, index) ->
                            for listener in listeners do
                                listener.Reply(snapshot, index)
                            return! loop(snapshot, index, [])
                        | Next (listener, _)
                        | Subscribe (listener) ->
                            return! start(listener :: listeners)
                    }

                start[]

            fun _ ->
                let rec loop(snapshot, index) =
                    seq {
                        yield snapshot
                        yield!
                            hub.PostAndReply(fun reply -> Next(reply, index))
                            |> loop
                    }
                hub.PostAndReply(Subscribe)
                |> loop