namespace Nata.IO.File

open System
open System.IO
open System.Threading
open Nata.IO

module Stream =

    type Settings = unit
    type Index = int
    type Path = string

    type private Message = AsyncReplyChannel<unit> * string option
    
    let create (path:Path) =
    
        let openStream() =
            let mode = FileMode.OpenOrCreate
            let access = FileAccess.ReadWrite
            let share = FileShare.ReadWrite
            new FileStream(path,mode,access,share)

        let writer = new StreamWriter(openStream())

        let count() =
            seq {
                use reader = new StreamReader(openStream())
                while (not reader.EndOfStream) do
                    yield reader.ReadLine()
            } |> Seq.length

        let lines = ref (count())

        let actor = MailboxProcessor<Message>.Start <| fun inbox ->
            let rec loop() =
                async {
                    let! sender, input = inbox.Receive()
                    match input with
                    | None ->
                        writer.Close()
                        sender.Reply()
                        return ()
                    | Some line ->
                        writer.WriteLine(line)
                        writer.Flush()
                        Interlocked.Increment(&lines.contents) |> ignore
                        sender.Reply()
                        return! loop()
                }
            loop()

        let write(line)=
            actor.PostAndReply(fun sender -> sender, Some line)

        let close() =
            actor.PostAndReply(fun sender -> sender, None)

        let read() =
            seq {
                use stream = openStream()
                use reader = new StreamReader(stream)
                
                let i = ref 0
                while (!i < !lines && not reader.EndOfStream) do
                    yield reader.ReadLine()
            }

        read,write,close