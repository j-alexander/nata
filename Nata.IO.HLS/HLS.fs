namespace Nata.IO.HLS

open System
open System.Text
open FFmpeg.AutoGen
open FFmpegSharp
//open EmguFFmpeg
//open OpenCvSharp
//open OpenCvSharp.Internal
open Nata.IO

type Client = VideoCapture


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Client =
    //
    // module CustomOpenCVSharpBuiltWithFFMPEG =
    //
    //     let create { Settings.Address=address } =
    //         new VideoCapture(address, VideoCaptureAPIs.FFMPEG) // VideoCaptureAPIs.ANY)
    //     
    //     let read (settings:Settings) =
    //         seq {
    //             use client = create settings
    //             
    //             if client.IsOpened() then
    //                 printfn "Stream opened successfully. Reading frames..."
    //                         
    //                 let rec readNextFrame () =
    //                     seq {
    //                         use mutable frame = new Mat()
    //
    //                         if client.Read(frame) then
    //                             if frame.Empty() then
    //                                 printfn "End of stream"
    //                                 () //end
    //                             else
    //                                 yield
    //                                     frame.Clone()
    //                                     |> Event.create
    //                                 yield! readNextFrame ()
    //                         else
    //                             printfn "Read failed"
    //                             ()
    //                     }
    //                 yield! readNextFrame()
    //             else
    //                 printfn "!!! Error: Could not open video stream"
    //                 yield! Seq.empty 
    //         }
    //
    //     let connect (settings:Settings) =
    //         [
    //             Capability.Reader <| fun () ->
    //                 read settings
    //         ]
            
    let read { Settings.Address=address } : seq<Event<MediaFrame>> =
        seq {
            use demuxer = MediaDemuxer.Open(address)
            use convert = new PixelConverter()
                    
            // 1. Get the list of video decoders for all streams in the demuxer
            let streamDecoders =
                demuxer
                |> Seq.map (fun (stream:MediaStream) ->
                    MediaDecoder.CreateDecoder(stream.CodecparRef))
                |> Seq.toList
            printfn "%d video stream decoders found" streamDecoders.Length 
            
            for packet in demuxer.ReadPackets() do
                match streamDecoders.[packet.StreamIndex] with
                | null ->
                    printfn "Null decoder %d" packet.StreamIndex
                | decoder when (decoder.CodecType <> Abstractions.AVMediaType.AVMEDIA_TYPE_VIDEO) ->
                    printfn "Some other decoder codec type %s" (decoder.CodecType.ToString())
                | decoder ->
                    if (decoder.CodecType = Abstractions.AVMediaType.AVMEDIA_TYPE_VIDEO) then
                        convert.SetOpts(decoder.Width, decoder.Height, Abstractions.AVPixelFormat.AV_PIX_FMT_BGR24)
                        for frame in decoder.DecodePacket(packet) do
                            for bgrFrame in convert.Convert(frame) do
                                let data = bgrFrame.GetData()
                                yield
                                    bgrFrame.Clone()
                                    |> Event.create

            yield! Seq.empty
        }
            
    let connect (settings:Settings) =
        [
            Capability.Reader <| fun () ->
                read settings
        ]