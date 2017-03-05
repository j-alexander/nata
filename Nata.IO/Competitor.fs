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
