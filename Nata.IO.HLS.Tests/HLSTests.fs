namespace Nata.IO.HLS.Tests

open System.Runtime.InteropServices

open FFmpegSharp
open FFmpeg.AutoGen
open FFmpeg.AutoGen.Bindings.DynamicallyLoaded
open NUnit.Framework

open Nata.IO
open Nata.IO.Channel
open Nata.IO.HLS

[<TestFixture>]
type HLSTests() =
    
    do
        RuntimeInformation.ProcessArchitecture
        |> printfn "Running on process architecture: %A"
        DynamicallyLoadedBindings.LibrariesPath <- "/opt/homebrew/lib"
        DynamicallyLoadedBindings.Initialize()
        ffmpeg.LibraryVersionMap
        |> Seq.iter (printfn "%A")
    
    let testAddress =
        "https://devstreaming-cdn.apple.com/videos/streaming/examples/img_bipbop_adv_example_fmp4/master.m3u8"

    let connect() =
        { Settings.Address=testAddress }
        |> HLS.Client.connect

    let profileMediaFrame(f: MediaFrame) =
        printfn "Frame Type: %A" f.PictType // e.g., AV_PICTURE_TYPE_I, AV_PICTURE_TYPE_P
        printfn "Presentation Timestamp (PTS): %d" f.Pts
        printfn "Frame Width: %d" f.Width
        printfn "Frame Height: %d" f.Height
        
        printfn "Data Planes (Components): %d" f.Data.Length
        printfn "Linesize (Stride) of Plane 0: %d bytes" f.Linesize.[0u]
        ()
    
    
    [<Test>]
    member x.TestRead() =
        let read =
            connect()
            |> reader

        read()
        |> Seq.take 3
        |> Seq.iter (fun { Event.Data = data } ->
            profileMediaFrame data)