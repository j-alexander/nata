namespace Nata.IO.HLS.Tests

open System
open System.IO
open System.Collections.Concurrent
open System.Reflection
open System.Threading
open System.Threading.Tasks
open FSharp.Data

open Nata.IO.Event

open OpenCvSharp
//open OpenCvSharp.
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Channel
open Nata.IO.HLS

[<TestFixture>]
type HLSTests() =

    let testAddress =
        "https://devstreaming-cdn.apple.com/videos/streaming/examples/img_bipbop_adv_example_fmp4/master.m3u8"

    let connect() = 
        { Settings.Address=testAddress }
        |> HLS.Client.connect

    let profileMat(f: Mat) = 
        printfn "--- Frame Profile Information ---"
        printfn $"Dimensions (Width x Height): {f.Width} x {f.Height} pixels"
        printfn $"Total Pixels: {f.Total()} pixels"
        printfn $"Channels: {f.Channels()} (Color Depth)"
        printfn $"Type (Internal OpenCV Code): {f.Type()}"
        printfn $"Depth (Internal OpenCV Code): {f.Depth()}"
        //printfn $"Total Data Size: {f.DataSize} bytes"
        printfn $"Is Continuous: {f.IsContinuous} (Indicates data stored in a single contiguous block)"
        printfn "---------------------------------"
    
    [<Test>]
    member x.TestRead() =
        let read =
            connect()
            |> reader

        read()
        |> Seq.take 100
        |> Seq.iter (fun { Event.Data = data } ->
            
            profileMat data)