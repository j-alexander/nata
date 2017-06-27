namespace Nata.IO

open Nata.Core

type Channel<'Data,'Index> = List<Capability<'Data,'Index>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Channel =

    let mapCapabilities fn : Channel<'DataIn,'IndexIn> -> Channel<'DataOut,'IndexOut> =
        List.map fn

    let mapData (codec:Codec<'DataIn,'DataOut>) : Channel<'DataIn,'Index> -> Channel<'DataOut,'Index> =
        List.map (Capability.mapData codec)

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>) : Channel<'Data,'IndexIn> -> Channel<'Data,'IndexOut> =
        List.map (Capability.mapIndex codec)

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>) : Channel<'DataIn,'IndexIn> -> Channel<'DataOut,'IndexOut> =
        mapData dataCodec
        >> mapIndex indexCodec

    let tryIndexer (channel:Channel<'Data,'Index>) : Indexer<'Index> option =
        channel |> List.tryPick (function Indexer x -> Some x | _ -> None)
        
    let tryWriter (channel:Channel<'Data,'Index>) : Writer<'Data> option =
        channel |> List.tryPick (function Writer x -> Some x | _ -> None)

    let tryWriterTo (channel:Channel<'Data,'Index>) : WriterTo<'Data,'Index> option =
        channel |> List.tryPick (function WriterTo x -> Some x | _ -> None)

    let tryReader (channel:Channel<'Data,'Index>) : Reader<'Data> option =
        channel |> List.tryPick (function Reader x -> Some x | _ -> None)

    let tryReaderFrom (channel:Channel<'Data,'Index>) : ReaderFrom<'Data,'Index> option =
        channel |> List.tryPick (function ReaderFrom x -> Some x | _ -> None)

    let trySubscriber (channel:Channel<'Data,'Index>) : Subscriber<'Data> option =
        channel |> List.tryPick (function Subscriber x -> Some x | _ -> None)

    let trySubscriberFrom (channel:Channel<'Data,'Index>) : SubscriberFrom<'Data,'Index> option =
        channel |> List.tryPick (function SubscriberFrom x -> Some x | _ -> None)

    let tryCompetitor (channel:Channel<'Data,'Index>) : Competitor<'Data> option =
        channel
        |> List.tryPick (function Competitor x -> Some x | _ -> None)
        |> function None -> Competitor.tryFallback(tryWriterTo channel, tryReaderFrom channel) | x -> x
        
    let indexer (channel:Channel<'Data,'Index>) =
        channel |> tryIndexer |> Option.get

    let writer (channel:Channel<'Data,'Index>) =
        channel |> tryWriter |> Option.get

    let writerTo (channel:Channel<'Data,'Index>) =
        channel |> tryWriterTo |> Option.get

    let reader (channel:Channel<'Data,'Index>) =
        channel |> tryReader |> Option.get

    let readerFrom (channel:Channel<'Data,'Index>) =
        channel |> tryReaderFrom |> Option.get

    let subscriber (channel:Channel<'Data,'Index>) =
        channel |> trySubscriber |> Option.get

    let subscriberFrom (channel:Channel<'Data,'Index>) =
        channel |> trySubscriberFrom |> Option.get

    let competitor (channel:Channel<'Data,'Index>) =
        channel |> tryCompetitor |> Option.get

    let read (channel:Channel<'Data,'Index>) =
        reader channel ()

    let subscribe (channel:Channel<'Data,'Index>) =
        subscriber channel ()