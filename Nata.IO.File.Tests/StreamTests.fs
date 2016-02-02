namespace Nata.IO.File.Tests

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

open NUnit.Framework
open Nata.IO

[<TestFixture>]
type StreamTests() =

    [<Test>]
    member x.TestFiles2() =

        let path = Guid.NewGuid().ToString("n") |> sprintf @"c:\users\jonathan\desktop\%s.txt" 
        let read,write,close = File.Stream.create path
        
        let messages = new BlockingCollection<string>()
        
        let format = sprintf "written line %d completed" 

        Task.Run(fun _ ->
            [1..1000000]
            |> Seq.iter (format >> write)
            messages.Add("Writer done!")) |> ignore
        [1..75]
        |> Seq.iter (fun worker ->
            Thread.Sleep(10)
            Task.Run(fun _ ->
                let count = ref 0
                for i, line in read() |> Seq.mapi(fun i x -> i+1,x) do
                    count := 1 + !count
                    if format i <> line then
                        messages.Add(sprintf "Worker %d finds #%d '%s' <> '%s'" worker i (format i) line)


                !count
                |> sprintf "Worker %d read %d" worker
                |> messages.Add)
            |> ignore)

        for i in [ 1 .. 76 ] do
            messages.Take() |> printfn "%s"

        close()