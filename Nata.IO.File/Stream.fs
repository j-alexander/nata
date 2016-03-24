namespace Nata.IO.File

open System
open System.Collections.Concurrent
open System.IO
open System.Text
open System.Threading
open FSharp.Data
open Nata.IO

module Stream =

    type Settings = unit
    type Index = int64
    type Path = string
    
    let [<Literal>] Empty = -1
    
    type private Result = Success of Index | Failure
    type private Message =
        | Close of AsyncReplyChannel<unit>
        | Write of AsyncReplyChannel<Result> * Event<JsonValue> * Position<Index>

    let create (path:Path) =
    
        let openStream() =
            let mode = FileMode.OpenOrCreate
            let access = FileAccess.ReadWrite
            let share = FileShare.ReadWrite
            new FileStream(path,mode,access,share)

        let writer = new StreamWriter(openStream())

        let encode, decode = Event.Codec.EventToString
        let tryDecode line =
            try Some (decode line)
            with _ -> None

        let count() =
            seq {
                use reader = new StreamReader(openStream())
                while (not reader.EndOfStream) do
                    yield reader.ReadLine()
            } |> Seq.choose tryDecode
              |> Seq.fold (fun i _ -> 1L + i) 0L

        let lines = ref (count())

        let actor = MailboxProcessor<Message>.Start <| fun inbox ->
            let rec loop() =
                async {
                    let! message = inbox.Receive()
                    match message with
                    | Close sender->
                        writer.Close()
                        sender.Reply()
                        return()
                    | Write (sender, event, position) ->

                        let insert() =
                            writer.WriteLine(encode event)
                            writer.Flush()
                            Interlocked.Increment(&lines.contents) |> ignore
                            sender.Reply(Success(!lines))
                        let fail() =
                            sender.Reply(Failure)

                        match position with
                        | End ->                           insert()
                        | Start when 0L = !lines ->        insert()
                        | At last when last+1L = !lines -> insert()
                        | _ ->                             fail()
                            
                        return! loop()
                }
            loop()

        let writeTo index event =
            match actor.PostAndReply(fun sender -> Write (sender, event, Position.At index)) with
            | Success index -> index
            | Failure -> raise (InvalidPosition(Position.At index))

        let write event =
            match actor.PostAndReply(fun sender -> Write (sender, event, Position.End)) with
            | Success index -> ()
            | Failure -> raise (InvalidPosition(Position.End))

        let close() =
            actor.PostAndReply(fun sender -> Close (sender))

        let readFrom(index:Index) =
            seq {
                use reader = new StreamReader(openStream())
                let i = ref -1L
                while (!i < !lines && not reader.EndOfStream) do
                    match reader.ReadLine() |> tryDecode with
                    | None -> ()
                    | Some event ->
                        i := 1L + !i
                        if !i >= index then
                            yield event, !i
            }

        let read() =
            readFrom 0L |> Seq.map fst

        let listenFrom(index) =
            seq {
                use stream = openStream()
                let builder = new StringBuilder()
                let i = ref -1L
                while true do
                    if stream.Position = stream.Length then
                        Thread.Sleep(1)
                    else
                        let x = stream.ReadByte()
                        if x = int '\n' then
                            match builder.ToString() |> tryDecode with
                            | None -> ()
                            | Some event ->
                                i := 1L + !i
                                if !i >= index then
                                    yield event, !i
                            builder.Clear() |> ignore
                        else
                            builder.Append(char x) |> ignore
            }

        let listen() =
            listenFrom 0L |> Seq.map fst
        
        [   
            Nata.IO.Capability.Reader
                read

            Nata.IO.Capability.ReaderFrom
                readFrom

            Nata.IO.Capability.Writer
                write

            Nata.IO.Capability.WriterTo
                writeTo

            Nata.IO.Capability.Subscriber
                listen

            Nata.IO.Capability.SubscriberFrom
                listenFrom
        ]
           


    let connect : Nata.IO.Connector<Settings,Path,JsonValue,Index> =
        
        fun settings ->
            let index = new ConcurrentDictionary<Path, Nata.IO.Capability<JsonValue,Index> list>()
            fun name ->
                index.GetOrAdd(name, create)