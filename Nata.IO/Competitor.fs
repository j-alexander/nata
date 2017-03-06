namespace Nata.IO

open Nata.Core

type Competitor<'Data> = (Event<'Data> -> Event<'Data>) -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Competitor =

    let mapData ((decode, encode):Codec<'DataIn,'DataOut>)
                (competitor:Competitor<'DataIn>) : Competitor<'DataOut> =
        fun fn ->
            competitor (Event.mapData decode >> fn >> Event.mapData encode)
            |> Seq.map (Event.mapData decode)

    let map = mapData

    let fallback (writeTo:WriterTo<'Data,'Index>)
                 (subscribeFrom:SubscriberFrom<'Data,'Index>) : Competitor<'Data> =
        fun (fn:Event<'Data>->Event<'Data>) ->
            let state() =
                subscribeFrom (Position.Before Position.End)
                |> Seq.head
            let update(e,i) =
                try writeTo (Position.After (Position.At i)) e |> Some
                with :? Position.Invalid<'Index> -> None
            let rec apply(last) =
                seq {
                    let eventIn, indexIn =
                        match last with
                        | Some (e,i) -> (e,i)
                        | None -> state()

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

    let tryFallback (writerTo, subscribeFrom) =
        writerTo
        |> Option.bind(fun writerTo ->
            subscribeFrom
            |> Option.map(fun subscribeFrom ->
                fallback writerTo subscribeFrom))