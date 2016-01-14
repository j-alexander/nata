namespace Nata.IO

type Capability<'Data,'Metadata,'Index> =
    | Writer            of Writer<'Data,'Metadata>
    | WriterTo          of WriterTo<'Data,'Metadata,'Index>
    | Reader            of Reader<'Data,'Metadata>
    | ReaderFrom        of ReaderFrom<'Data,'Metadata,'Index>
    | Subscriber        of Subscriber<'Data,'Metadata>
    | SubscriberFrom    of SubscriberFrom<'Data,'Metadata,'Index>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Capability =
    
    let mapData ((encode,decode):Codec<'DataIn,'DataOut>)
                (capability:Capability<'DataOut,'Metadata,'Index>) : Capability<'DataIn,'Metadata,'Index> =
        match capability with
        | Writer x ->         x |> Writer.mapData encode |> Writer
        | WriterTo x ->       x |> WriterTo.mapData encode |> WriterTo
        | Reader x ->         x |> Reader.mapData decode |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapData decode |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.mapData decode |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapData decode |> SubscriberFrom

    let mapMetadata ((encode,decode):Codec<'MetadataIn,'MetadataOut>)
                    (capability:Capability<'Data,'MetadataOut,'Index>) : Capability<'Data,'MetadataIn,'Index> =
        match capability with
        | Writer x ->         x |> Writer.mapMetadata encode |> Writer
        | WriterTo x ->       x |> WriterTo.mapMetadata encode |> WriterTo
        | Reader x ->         x |> Reader.mapMetadata decode |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapMetadata decode |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.mapMetadata decode |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapMetadata decode |> SubscriberFrom

    let map ((encodeData,decodeData):Codec<'DataIn,'DataOut>)
            ((encodeMetadata,decodeMetadata):Codec<'MetadataIn,'MetadataOut>)
            ((encodeIndex,decodeIndex):Codec<'IndexIn,'IndexOut>)
            (capability:Capability<'DataOut,'MetadataOut,'IndexOut>) : Capability<'DataIn,'MetadataIn,'IndexIn> =
        match capability with
        | Writer x ->         x |> Writer.map encodeData encodeMetadata |> Writer
        | WriterTo x ->       x |> WriterTo.map encodeData encodeMetadata (encodeIndex,decodeIndex) |> WriterTo
        | Reader x ->         x |> Reader.map decodeData decodeMetadata |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.map decodeData decodeMetadata (decodeIndex,encodeIndex) |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.map decodeData decodeMetadata |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.map decodeData decodeMetadata (decodeIndex,encodeIndex) |> SubscriberFrom

    let tryReader (capabilities:Capability<'Data,'Metadata,'Index> list) : Reader<'Data,'Metadata> option =
        capabilities |> List.tryPick (function Reader x -> Some x | _ -> None)

    let tryReaderFrom (capabilities:Capability<'Data,'Metadata,'Index> list) : ReaderFrom<'Data,'Metadata,'Index> option =
        capabilities |> List.tryPick (function ReaderFrom x -> Some x | _ -> None)

    let tryWriter (capabilities:Capability<'Data,'Metadata,'Index> list) : Writer<'Data,'Metadata> option =
        capabilities |> List.tryPick (function Writer x -> Some x | _ -> None)

    let tryWriterTo (capabilities:Capability<'Data,'Metadata,'Index> list) : WriterTo<'Data,'Metadata,'Index> option =
        capabilities |> List.tryPick (function WriterTo x -> Some x | _ -> None)

    let trySubscriber (capabilities:Capability<'Data,'Metadata,'Index> list) : Subscriber<'Data,'Metadata> option =
        capabilities |> List.tryPick (function Subscriber x -> Some x | _ -> None)

    let trySubscriberFrom (capabilities:Capability<'Data,'Metadata,'Index> list) : SubscriberFrom<'Data,'Metadata,'Index> option =
        capabilities |> List.tryPick (function SubscriberFrom x -> Some x | _ -> None)
        
    let reader (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> tryReader |> Option.get

    let readerFrom (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> tryReaderFrom |> Option.get

    let writer (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> tryWriter |> Option.get

    let writerTo (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> tryWriterTo |> Option.get

    let subscriber (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> trySubscriber |> Option.get

    let subscriberFrom (capabilities:Capability<'Data,'Metadata,'Index> list) =
        capabilities |> trySubscriberFrom |> Option.get