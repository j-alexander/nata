namespace Nata.IO.EventHub.Tests

open System
open NUnit.Framework
open Nata.Core
open Nata.IO
open Nata.IO.Capability
open Nata.IO.EventHub

type MaskEnvelope =
    { mask : string
      data : byte[] }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MaskEnvelope =
    
    module Codec =
        
        let MaskEnvelopeToBytes : Codec<MaskEnvelope,byte[]> = JsonValue.Codec.createTypeToBytes()
        let BytesToMaskEnvelope : Codec<byte[],MaskEnvelope> = Codec.reverse MaskEnvelopeToBytes

    let toBytes,ofBytes = Codec.MaskEnvelopeToBytes

    let create mask data = { mask=mask; data=data }
    let choose mask = function
        | { mask=m; data=d } when (mask=m)-> Some d
        | _ -> None

    let encode mask = create mask >> toBytes
    let decode mask bytes = 
        try bytes |> ofBytes |> choose mask
        with _ -> None

    let mapCapability mask =
        let encode = encode mask
        let decode = decode mask
        function
        | Indexer x ->        x |> Indexer
        | Writer x ->         x |> Writer.mapData encode            |> Writer
        | WriterTo x ->       x |> WriterTo.mapData encode          |> WriterTo
        | Reader x ->         x |> Reader.chooseData decode         |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.chooseData decode     |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.chooseData decode     |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.chooseData decode |> SubscriberFrom