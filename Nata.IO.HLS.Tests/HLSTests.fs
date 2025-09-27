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
        // Print the full build configuration string
        let buildInfo = Cv2.GetBuildInformation()
        printfn "%s" buildInfo
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
    
    (*
    On MacOS, for the version of the runtime I was using, I copied it local so that the tests could find it.
    sudo cp ~/.nuget/packages/opencvsharp4.runtime.osx_arm64/4.8.1/runtimes/osx-arm64/native/*.dylib /usr/local/lib/    

    There's probably a better way to do this, for example, I tried the following item group in the test project file:
    
      <ItemGroup>
        <Content Include="$(NuGetPackageRoot)opencvsharp4.runtime.osx_arm64/4.8.1/runtimes/osx-arm64/native/*.dylib"
                 CopyToOutputDirectory="Always" />
      </ItemGroup>
      
    It successfully pulls them into the output folder, but for some reason the test runner on rider with "dotnet test"
    can't find them in the output folder.
    *)
    
    [<Test>]
    member x.TestRead() =
        let read =
            connect()
            |> reader

        read()
        |> Seq.take 100
        |> Seq.iter (fun { Event.Data = data } ->
            
            profileMat data)