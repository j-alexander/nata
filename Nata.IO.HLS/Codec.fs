module Nata.IO.HLS.Codec

open System
open SkiaSharp
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open FFmpeg.AutoGen
open FFmpegSharp
open Nata.Core

let fromMediaFrameToBitmap(frame:MediaFrame) =
    
    if frame.Format <> int AVPixelFormat.AV_PIX_FMT_BGRA then
        failwithf "Frame pixel format must be BGRA, but got %A" frame.Format

    let width = frame.Width
    let height = frame.Height
    let stride = frame.Linesize.[0u]

    // Create SKBitmap (BGRA8888)
    let bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul)
    use pixmap = bmp.PeekPixels()
    let bmpPtr = pixmap.GetPixels()
    let bmpRowBytes = pixmap.RowBytes

    // Direct copy since formats match
    for y in 0 .. height - 1 do
        let srcRowPtr = NativePtr.add frame.Data.[0u] (y * stride)
        let dstPtr = IntPtr.Add(bmpPtr, y * bmpRowBytes)
        
        // Direct memory copy using correct Marshal.Copy signature
        let copySize = min (width * 4) bmpRowBytes
        let srcIntPtr = NativePtr.toNativeInt srcRowPtr
        
        // Copy from source IntPtr to destination IntPtr
        Buffer.MemoryCopy(srcIntPtr.ToPointer(), dstPtr.ToPointer(), copySize, copySize)
        
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