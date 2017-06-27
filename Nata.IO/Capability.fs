namespace Nata.IO

open Nata.Core

type Capability<'Data,'Index> =
    | Indexer           of Indexer<'Index>
    | Writer            of Writer<'Data>
    | WriterTo          of WriterTo<'Data,'Index>
    | Reader            of Reader<'Data>
    | ReaderFrom        of ReaderFrom<'Data,'Index>
    | Subscriber        of Subscriber<'Data>
    | SubscriberFrom    of SubscriberFrom<'Data,'Index>
    | Competitor        of Competitor<'Data>
    

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
        | Competitor x ->     x |> Competitor.mapData (encode,decode) |> Competitor

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
        | Competitor x ->     x |> Competitor

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (capability:Capability<'DataIn,'IndexIn>) : Capability<'DataOut,'IndexOut> =
        capability
        |> mapData dataCodec
        |> mapIndex indexCodec
        