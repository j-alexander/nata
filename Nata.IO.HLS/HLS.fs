namespace Nata.IO.HLS

open System
open System.Text
open OpenCvSharp
open Nata.IO

type Client = VideoCapture


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Client =

    let create { Settings.Address=address } =
        new VideoCapture(address, VideoCaptureAPIs.ANY)
    
    let read (settings:Settings) =
        seq {
            use client = create settings 
            if client.IsOpened() then
                printfn "Stream opened successfully. Reading frames..."
                        
                let rec readNextFrame () =
                    seq {
                        use mutable frame = new Mat()

                        if client.Read(frame) then
                            if frame.Empty() then
                                printfn "End of stream"
                                () //end
                            else
                                yield
                                    frame.Clone()
                                    |> Event.create
                                yield! readNextFrame ()
                        else
                            printfn "Read failed"
                            ()
                    }
                yield! readNextFrame()
            else
                printfn "!!! Error: Could not open video stream"
                yield! Seq.empty 
        }

    let connect (settings:Settings) =
        [
            Capability.Reader <| fun () ->
                read settings
        ]