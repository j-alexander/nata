namespace Nata.IO

type Capability<'Data,'Index> =
    | Indexer           of Indexer<'Index>
    | Writer            of Writer<'Data>
    | WriterTo          of WriterTo<'Data,'Index>
    | Reader            of Reader<'Data>
    | ReaderFrom        of ReaderFrom<'Data,'Index>
    | Subscriber        of Subscriber<'Data>
    | SubscriberFrom    of SubscriberFrom<'Data,'Index>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Capability =
    
    let mapData ((encode,decode):Codec<'DataIn,'DataOut>)
                (capability:Capability<'DataIn,'Index>) : Capability<'DataOut,'Index> =
        match capability with
        | Indexer x ->        x |> Indexer
        | Writer x ->         x |> Writer.mapData decode |> Writer
        | WriterTo x ->       x |> WriterTo.mapData decode |> WriterTo
        | Reader x ->         x |> Reader.mapData encode |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapData encode |> ReaderFrom
        | Subscriber x ->     x |> Subscriber.mapData encode |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapData encode |> SubscriberFrom

    let mapIndex (codec:Codec<'IndexIn, 'IndexOut>)
                 (capability:Capability<'Data,'IndexIn>) : Capability<'Data,'IndexOut> =
        match capability with
        | Indexer x ->        x |> Indexer.mapIndex (Codec.reverse codec) |> Indexer
        | Writer x ->         x |> Writer
        | WriterTo x ->       x |> WriterTo.mapIndex (Codec.reverse codec) |> WriterTo
        | Reader x ->         x |> Reader
        | ReaderFrom x ->     x |> ReaderFrom.mapIndex codec |> ReaderFrom
        | Subscriber x ->     x |> Subscriber
        | SubscriberFrom x -> x |> SubscriberFrom.mapIndex codec |> SubscriberFrom

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (capability:Capability<'DataIn,'IndexIn>) : Capability<'DataOut,'IndexOut> =
        capability
        |> mapData dataCodec
        |> mapIndex indexCodec
        
    let tryIndexer (capabilities:Capability<'Data,'Index> list) : Indexer<'Index> option =
        capabilities |> List.tryPick (function Indexer x -> Some x | _ -> None)
        
    let tryWriter (capabilities:Capability<'Data,'Index> list) : Writer<'Data> option =
        capabilities |> List.tryPick (function Writer x -> Some x | _ -> None)

    let tryWriterTo (capabilities:Capability<'Data,'Index> list) : WriterTo<'Data,'Index> option =
        capabilities |> List.tryPick (function WriterTo x -> Some x | _ -> None)

    let tryReader (capabilities:Capability<'Data,'Index> list) : Reader<'Data> option =
        capabilities |> List.tryPick (function Reader x -> Some x | _ -> None)

    let tryReaderFrom (capabilities:Capability<'Data,'Index> list) : ReaderFrom<'Data,'Index> option =
        capabilities |> List.tryPick (function ReaderFrom x -> Some x | _ -> None)

    let trySubscriber (capabilities:Capability<'Data,'Index> list) : Subscriber<'Data> option =
        capabilities |> List.tryPick (function Subscriber x -> Some x | _ -> None)

    let trySubscriberFrom (capabilities:Capability<'Data,'Index> list) : SubscriberFrom<'Data,'Index> option =
        capabilities |> List.tryPick (function SubscriberFrom x -> Some x | _ -> None)
        
    let indexer (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryIndexer |> Option.get

    let writer (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryWriter |> Option.get

    let writerTo (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryWriterTo |> Option.get

    let reader (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryReader |> Option.get

    let readerFrom (capabilities:Capability<'Data,'Index> list) =
        capabilities |> tryReaderFrom |> Option.get

    let subscriber (capabilities:Capability<'Data,'Index> list) =
        capabilities |> trySubscriber |> Option.get

    let subscriberFrom (capabilities:Capability<'Data,'Index> list) =
        capabilities |> trySubscriberFrom |> Option.get

    let read (capabilities:Capability<'Data,'Index> list) =
        reader capabilities ()

    let subscribe (capabilities:Capability<'Data,'Index> list) =
        subscriber capabilities ()