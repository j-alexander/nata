namespace Nata.IO.HLS

open System
open System.Text
open Nata.IO

type Client = unit


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Client =

    let create { Settings.Address=address } =
        ()
    
        
    let read (client:Client) prefix =
        Seq.empty

    let connect (settings:Settings) =
        let client = create settings
        fun prefix ->
            [
                Capability.Reader <| fun () ->
                    read client prefix
            ]