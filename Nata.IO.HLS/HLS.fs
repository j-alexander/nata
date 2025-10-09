namespace Nata.IO.HLS

open System.IO
open FFmpeg.AutoGen
open FFmpeg.AutoGen.Bindings.DynamicallyLoaded
open FFmpegSharp
open NLog
open Nata.IO

type Client = VideoCapture

type Settings = {
    HttpLiveStreamingAddress : string
    FFmpeg71DynamicLibraryPath : DirectoryInfo
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Client =
            
    let log = LogManager.GetLogger("Nata.IO.HLS.Client")
    
    let read { Settings.HttpLiveStreamingAddress = address } : seq<Event<MediaFrame>> =
        seq {
            use demuxer = MediaDemuxer.Open(address)
            use convert = new PixelConverter()

            // 1. Inspect available streams and find video streams
            let videoStreams =
                demuxer
                |> Seq.mapi (fun i stream -> i, stream)
                |> Seq.choose (fun (i, stream) ->
                    if stream.CodecparRef.codec_type = Abstractions.AVMediaType.AVMEDIA_TYPE_VIDEO then
                        Some(i, stream.CodecparRef.width, stream.CodecparRef.height)
                    else None)
                |> Seq.toList

            if videoStreams.IsEmpty then
                failwith "No video streams found"

            // 2. Select the highest-resolution stream
            let selectedStreamIndex =
                videoStreams
                |> List.maxBy (fun (_, w, h) -> w * h) // pick stream with largest width*height
                |> fun (i, _, _) -> i

            log.Info(sprintf "Selected video stream %d for decoding" selectedStreamIndex)

            // 3. Create decoders for all streams (needed for packet index mapping)
            let streamDecoders =
                demuxer
                |> Seq.map (fun stream -> MediaDecoder.CreateDecoder(stream.CodecparRef))
                |> Seq.toList

            // 4. Decode only the selected stream
            for packet in demuxer.ReadPackets() do
                if packet.StreamIndex = selectedStreamIndex then
                    let decoder = streamDecoders.[packet.StreamIndex]
                    convert.SetOpts(decoder.Width, decoder.Height, Abstractions.AVPixelFormat.AV_PIX_FMT_BGRA)
                    for frame in decoder.DecodePacket(packet) do
                        for bgraFrame in convert.Convert(frame) do
                            yield bgraFrame.Clone() |> Event.create
        }
            
    let initialize (path:DirectoryInfo) =
        if (path.Exists |> not) then
            path.FullName
            |> sprintf "FFmpeg 7.1 does not exist at %A"
            |> failwith
        DynamicallyLoadedBindings.LibrariesPath <- path.FullName
        DynamicallyLoadedBindings.Initialize()
        ffmpeg.LibraryVersionMap
        |> Seq.iter (sprintf "%A" >> log.Info)
    
    let connect (settings:Settings) =
        initialize settings.FFmpeg71DynamicLibraryPath
        [
            Capability.Reader <| fun () ->
                read settings
        ]