namespace Nata.IO.HLS.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open FSharp.Data

open Nata.IO.Event

open FFmpeg.Loader
//open OpenCvSharp
open FFmpegSharp
open FFmpeg.AutoGen
open FFmpeg.AutoGen.Bindings
open FFmpeg.AutoGen.Bindings.DynamicallyLoaded
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.HLS

[<TestFixture>]
type HLSTests() =
    
    let setup  =
        let arch = RuntimeInformation.ProcessArchitecture;
        printfn "Running on process architecture: %A" arch
        
        //FFmpeg.AutoGen.ffmpeg.RootPath <- "/opt/homebrew/lib"
        DynamicallyLoadedBindings.LibrariesPath <- "/opt/homebrew/lib"
        DynamicallyLoadedBindings.Initialize()
        //FFmpeg.AutoGen.ffmpeg.avdevice_register_all()
    
        //printfn "FFmpeg version info: %A" (ffmpeg.av_version_info())

        //printfn "Number of codecs: %A" (ffmpeg.av_codec_count())
        for x in FFmpeg.AutoGen.ffmpeg.LibraryVersionMap do
            printfn "%A" x
    
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
        |> Seq.take 100
        |> Seq.iter (fun { Event.Data = data } ->
            
            profileMediaFrame data)