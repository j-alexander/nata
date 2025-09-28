module Nata.IO.HLS.Codec

open System
open SkiaSharp
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open FFmpeg.AutoGen
open FFmpegSharp
open Nata.Core

let fromMediaFrameToBitmap(frame:MediaFrame) =
    
    
    if frame.Format <> int AVPixelFormat.AV_PIX_FMT_BGR24 then
        failwithf "Frame pixel format must be BGR24, but got %A" frame.Format

    let width = frame.Width
    let height = frame.Height
    let stride = frame.Linesize.[0u]

    // Create SKBitmap (BGRA8888)
    let bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul)
    use pixmap = bmp.PeekPixels()
    let bmpPtr = pixmap.GetPixels()  // nativeint
    let bmpRowBytes = pixmap.RowBytes

    // Temporary buffer for one row in BGRA
    let rowBytes = Array.zeroCreate<byte> (width * 4)

    for y in 0 .. height - 1 do
        // Correct way: offset nativeptr<byte> by elements
        let srcRowPtr : nativeptr<byte> = NativePtr.add frame.Data.[0u] (y * stride)

        // Convert BGR -> BGRA
        for x in 0 .. width - 1 do
            let srcPixel = NativePtr.add srcRowPtr (x * 3)
            let b = NativePtr.read srcPixel
            let g = NativePtr.read (NativePtr.add srcPixel 1)
            let r = NativePtr.read (NativePtr.add srcPixel 2)

            let idx = x * 4
            rowBytes.[idx] <- b
            rowBytes.[idx + 1] <- g
            rowBytes.[idx + 2] <- r
            rowBytes.[idx + 3] <- 255uy

        // Copy row into Skia bitmap memory
        let dstPtr = IntPtr.Add(bmpPtr, y * bmpRowBytes)
        Marshal.Copy(rowBytes, 0, dstPtr, width * 4)
        
    bmp
    
//
// let MediaFrameToBitmap : Codec<MediaFrame, Bitmap> = JsonValue.Codec.JsonValueToBytes
// let BitmapToMediaFrame = Codec.reverse JsonValueToValue
//
// let KeyStringToKeyValue : Codec<Key*string, KeyValue> =
//     let encode, decode = StringToValue
//     (fun (k,v) -> k, encode v),
//     (fun (k,v) -> k, decode v)
// let KeyValueToKeyString = Codec.reverse KeyStringToKeyValue