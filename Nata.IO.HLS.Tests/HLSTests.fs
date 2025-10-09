namespace Nata.IO.HLS.Tests

open System
open System.Diagnostics
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
        let path =
            match RuntimeInformation.ProcessArchitecture with
            | Architecture.Arm64 -> "/opt/homebrew/lib"
            | Architecture.X64 ->  @"C:\Program Files\ffmpeg-7.1.1-full_build-shared\bin"
            | os ->  failwithf "New architecture (%A) - this test needs to be updated." os
        if (not (Directory.Exists(path))) then
            failwithf "FFmpeg 7.1 does not exist at %A" path
        DynamicallyLoadedBindings.LibrariesPath <- path
        DynamicallyLoadedBindings.Initialize()
        ffmpeg.LibraryVersionMap
        |> Seq.iter (printfn "%A")
    
    let log = LogManager.GetLogger("Nata.IO.HLS.Tests.HLSTests")
    
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
    
    let convert (f:MediaFrame) =
        Codec.fromMediaFrameToBitmap f
        
    let captureMediaFrame i (bitmap:SKBitmap) =
        use file = File.OpenWrite(sprintf "/Users/Jonathan/Desktop/Output/%08d.png" i)
        bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(file)
    
    [<SetUp>]
    member x.SetUp() =
        let config = new LoggingConfiguration()
        let consoleTarget = new ConsoleTarget("logconsole") :> Target
        config.AddTarget(consoleTarget)
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "*")
        LogManager.Configuration <- config
    
    (*
On M3 MacBook Air 24G
2025-09-28 16:02:34.3330|INFO|Nata.IO.HLS.Tests.HLSTests|Starting
2025-09-28 16:02:34.3377|INFO|Nata.IO.HLS.Tests.HLSTests|Create required 00:00:00.0008115
2025-09-28 16:02:43.0970|INFO|Nata.IO.HLS.Client|Selected video stream 1 for decoding
2025-09-28 16:02:54.3557|INFO|Nata.IO.HLS.Tests.HLSTests|Stream required 00:00:20.0177841
2025-09-28 16:02:55.4002|INFO|Nata.IO.HLS.Tests.HLSTests|Conversion required 00:00:01.0443470
2025-09-28 16:03:16.3706|INFO|Nata.IO.HLS.Tests.HLSTests|Save required 00:00:20.9695205

On 9950X 64G
2025-10-08 22:19:25.8820|INFO|Nata.IO.HLS.Tests.HLSTests|Starting
2025-10-08 22:19:25.8820|INFO|Nata.IO.HLS.Tests.HLSTests|Create required 00:00:00.0011766
2025-10-08 22:19:37.4102|INFO|Nata.IO.HLS.Client|Selected video stream 1 for decoding
2025-10-08 22:19:39.8954|INFO|Nata.IO.HLS.Tests.HLSTests|Stream required 00:00:14.0074642
2025-10-08 22:19:40.6886|INFO|Nata.IO.HLS.Tests.HLSTests|Conversion required 00:00:00.7929683
2025-10-08 22:19:58.4751|INFO|Nata.IO.HLS.Tests.HLSTests|Save required 00:00:17.7866143
    *)
    [<Test>]
    member x.TestRead() =
        log.Info("Starting")
        
        let watchCreate = Stopwatch.StartNew()
        let read =
            connect()
            |> reader
        watchCreate.Stop()
        log.Info(sprintf "Create required %A" watchCreate.Elapsed)
        
        let watchStream = Stopwatch.StartNew()
        let frames = 
            read()
            |> Seq.take 500
            |> Seq.toList
        watchStream.Stop()
        log.Info(sprintf "Stream required %A" watchStream.Elapsed)
        
        let watchConversion = Stopwatch.StartNew()
        let bitmaps =
            frames
            |> List.map (Event.data >> Codec.fromMediaFrameToBitmap)
        watchConversion.Stop()
        log.Info(sprintf "Conversion required %A" watchConversion.Elapsed)
        
        let watchSave = Stopwatch.StartNew()
        bitmaps 
        |> List.iteri captureMediaFrame
        watchSave.Stop()
        log.Info(sprintf "Save required %A" watchSave.Elapsed)