namespace Nata.IO.File

open System
open System.IO
open System.Threading
open Nata.IO

module Stream =

    type Settings = unit
    type Index = int
    type Path = string

    
    let create (path:Path) =
    
        let stream =
            let mode = FileMode.OpenOrCreate
            let access = FileAccess.ReadWrite
            let share = FileShare.ReadWrite
            new FileStream(path,mode,access,share)
        let writer = new StreamWriter(stream)

        let count() =
            use stream =
                let mode = FileMode.Open
                let access = FileAccess.ReadWrite
                let share = FileShare.ReadWrite
                new FileStream(path,mode,access,share)
            use reader = new StreamReader(stream)
                
            let i = ref 0
            while (not reader.EndOfStream) do
                i := 1 + !i
            !i

        let lines = ref (count())

        let actor = MailboxProcessor<string option>.Start <| fun inbox ->
            let rec loop() =
                async {
                    let! input = inbox.Receive()
                    match input with
                    | None ->
                        writer.Close()
                        stream.Close()
                        return ()
                    | Some line ->
                        writer.WriteLine(line)
                        writer.Flush()
                        Interlocked.Increment(&lines.contents) |> ignore
                        return! loop()
                }
            loop()

        let write(line)=
            Some line |> actor.Post

        let close() =
            None |> actor.Post

        let read() =
            seq {
                use stream =
                    let mode = FileMode.Open
                    let access = FileAccess.ReadWrite
                    let share = FileShare.ReadWrite
                    new FileStream(path,mode,access,share)
                use reader = new StreamReader(stream)
                
                let i = ref 0
                while (!i < !lines && not reader.EndOfStream) do
                    yield reader.ReadLine()
            }

        read,write,close