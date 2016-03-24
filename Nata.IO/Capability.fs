namespace Nata.IO

type Capability<'Data,'Index> =
    | Writer            of Writer<'Data>
    | WriterTo          of WriterTo<'Data,'Index>
    | Reader            of Reader<'Data>
    | ReaderFrom        of ReaderFrom<'Data,'Index>
    | Subscriber        of Subscriber<'Data>
    | SubscriberFrom    of SubscriberFrom<'Data,'Index>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Capability =
    
    let mapData ((encode,decode):Codec<'DataIn,'DataOut>)
                (capability:Capability<'DataOut,'Index>) : Capability<'DataIn,'Index> =
        match capability with
        | Writer x ->         x |> Writer.mapData encode |> Writer
        | WriterTo x ->       x |> WriterTo.mapData encode |> WriterTo
        | Reader x ->         x |> Reader.mapData decode |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapData decode |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.mapData decode |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapData decode |> SubscriberFrom

    let mapIndex (codec:Codec<'IndexIn, 'IndexOut>)
                 (capability:Capability<'Data,'IndexOut>) : Capability<'Data,'IndexIn> =
        match capability with
        | Writer x ->         x |> Writer
        | WriterTo x ->       x |> WriterTo.mapIndex codec |> WriterTo
        | Reader x ->         x |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapIndex (Codec.reverse codec) |> ReaderFrom
        | Subscriber x ->     x |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapIndex (Codec.reverse codec) |> SubscriberFrom

    let map ((encodeData,decodeData):Codec<'DataIn,'DataOut>)
            ((encodeIndex,decodeIndex):Codec<'IndexIn,'IndexOut>)
            (capability:Capability<'DataOut,'IndexOut>) : Capability<'DataIn,'IndexIn> =
        match capability with
        | Writer x ->         x |> Writer.map encodeData |> Writer
        | WriterTo x ->       x |> WriterTo.map encodeData (encodeIndex,decodeIndex) |> WriterTo
        | Reader x ->         x |> Reader.map decodeData |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.map decodeData (decodeIndex,encodeIndex) |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.map decodeData |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.map decodeData (decodeIndex,encodeIndex) |> SubscriberFrom

    let tryReader (capabilities:Capability<'Data,'Index> list) : Reader<'Data> option =
        capabilities |> List.tryPick (function Reader x -> Some x | _ -> None)

    let tryReaderFrom (capabilities:Capability<'Data,'Index> list) : ReaderFrom<'Data,'Index> option =
        capabilities |> List.tryPick (function ReaderFrom x -> Some x | _ -> None)

    let tryWriter (capabilities:Capability<'Data,'Index> list) : Writer<'Data> option =
        capabilities |> List.tryPick (function Writer x -> Some x | _ -> None)

    let tryWriterTo (capabilities:Capability<'Data,'Index> list) : WriterTo<'Data,'Index> option =
        capabilities |> List.tryPick (function WriterTo x -> Some x | _ -> None)

    let trySubscriber (capabilities:Capability<'Data,'Index> list) : Subscriber<'Data> option =
        capabilities |> List.tryPick (function Subscriber x -> Some x | _ -> None)

    let trySubscriberFrom (capabilities:Capability<'Data,'Index> list) : SubscriberFrom<'Data,'Index> option =
        capabilities |> List.tryPick (function SubscriberFrom x -> Some x | _ -> None)
        
    let reader (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryReader |> Option.get

    let readerFrom (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryReaderFrom |> Option.get

    let writer (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryWriter |> Option.get

    let writerTo (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryWriterTo |> Option.get

    let subscriber (capabilities:Capability<'Data,'Index> list) =
        capabilities |> trySubscriber |> Option.get

    let subscriberFrom (capabilities:Capability<'Data,'Index> list) =
        capabilities |> trySubscriberFrom |> Option.get

    let read (capabilities:Capability<'Data,'Index> list) =
        reader capabilities ()

    let subscribe (capabilities:Capability<'Data,'Index> list) =
        subscriber capabilities ()