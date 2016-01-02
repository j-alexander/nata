open System
open API.Service

[<EntryPoint>]
let main argv = 

    Console.WriteLine("Executing")
    let ws = defaultEndpoint |> start
    Console.WriteLine("Press enter to stop.")
    Console.ReadLine() |> ignore

    0

