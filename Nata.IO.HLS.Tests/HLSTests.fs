namespace Nata.IO.HLS.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks
open FSharp.Data

open Nata.IO.Event

open FFmpeg.Loader
//open OpenCvSharp
open FFmpegSharp
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.HLS

[<TestFixture>]
type HLSTests() =
    
    let setup  =
        let value = HLS.Setup.run()
        printfn "HLS.Setup report: %s" value
        value
    
    let testAddress =
        "https://devstreaming-cdn.apple.com/videos/streaming/examples/img_bipbop_adv_example_fmp4/master.m3u8"
        //"file:/Users/jonathan/Desktop/9c7e9d0b-978b-4bc7-a25d-4488323dd495.mp4"

    let connect() =
        // Print the full build configuration string
        //let buildInfo = Cv2.GetBuildInformation()
        //printfn "%s" buildInfo
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