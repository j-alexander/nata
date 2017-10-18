namespace Nata.IO

open Nata.Core

type Competitor<'Data> = (Event<'Data> option -> Event<'Data>) -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Competitor =

    let mapData ((decode, encode):Codec<'DataIn,'DataOut>)
                (competitor:Competitor<'DataIn>) : Competitor<'DataOut> =
        fun fn ->
            competitor (Option.map (Event.mapData decode) >> fn >> Event.mapData encode)
            |> Seq.map (Event.mapData decode)

    let map = mapData

    let fallback (writeTo:WriterTo<'Data,'Index>)
                 (readFrom:ReaderFrom<'Data,'Index>) : Competitor<'Data> =
        fun (fn:Event<'Data> option->Event<'Data>) ->
            let state() =
                readFrom (Position.Before Position.End)
                |> Seq.tryHead
            let update(event,index) =
                let position =
                    index
                    |> Option.map (Position.At >> Position.After)
                    |> Option.defaultValue (Position.Start)
                try writeTo position event |> Some
                with :? Position.Invalid<'Index> -> None
            let rec apply(last) =
                seq {
                    let eventIn, indexIn =
                        last
                        |> Option.coalesceWith state
                        |> Option.distribute

                    let eventOut = fn eventIn
                    let result =
                        update(eventOut, indexIn)
                        |> Option.map (fun indexOut -> eventOut, indexOut)
                    match result with
                    | None -> ()
                    | Some _ ->
                        yield eventOut
                    yield! apply result
                }
            apply(None)

    let tryFallback (writerTo, readerFrom) =
        writerTo
        |> Option.bind(fun writerTo ->
            readerFrom
            |> Option.map(fun readerFrom ->
                fallback writerTo readerFrom))