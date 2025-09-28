namespace Nata.IO.HLS.Tests

open System
open System.IO
open System.Runtime.InteropServices

open FFmpegSharp
open FFmpeg.AutoGen
open FFmpeg.AutoGen.Bindings.DynamicallyLoaded
open NUnit.Framework
open SkiaSharp

open NLog
open NLog.Config
open NLog.Targets

open Nata.Core
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

    let profileMediaFrame i (f: MediaFrame) =
        printfn "Frame Type: %A" f.PictType
        printfn "Presentation Timestamp (PTS): %d" f.Pts
        printfn "Frame Width: %d" f.Width
        printfn "Frame Height: %d" f.Height
        
        printfn "Data Planes (Components): %d" f.Data.Length
        printfn "Linesize (Stride) of Plane 0: %d bytes" f.Linesize.[0u]
    
    let captureMediaFrame i (f:MediaFrame) =
        let r = Codec.fromMediaFrameToBitmap f
        use file = File.OpenWrite(sprintf "/Users/jonathan/Desktop/Output/%08d.png" i)
        r.Encode(SKEncodedImageFormat.Png, 100).SaveTo(file)
    
    [<SetUp>]
    member x.SetUp() =
        let config = new LoggingConfiguration()
        let consoleTarget = new ConsoleTarget("logconsole") :> Target
        config.AddTarget(consoleTarget)
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "*")
        LogManager.Configuration <- config
    
    [<Test>]
    member x.TestRead() =
        let read =
            connect()
            |> reader

        read()
        |> Seq.take 500
        |> Seq.map Event.data
        |> Seq.iteri profileMediaFrame