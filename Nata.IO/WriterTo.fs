namespace Nata.IO

open Nata.Core

type WriterTo<'Data,'Index> = Position<'Index> -> Event<'Data> -> 'Index

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WriterTo =

    let mapData (fn:'DataIn->'DataOut)
                (writerTo:WriterTo<'DataOut,'Index>) : WriterTo<'DataIn,'Index> =
        fun index ->
            Event.mapData fn >> writerTo index

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (writerTo:WriterTo<'Data,'IndexOut>) : WriterTo<'Data,'IndexIn> =
        Position.map encode >> fun position ->
            Position.applyMap decode (writerTo position >> decode)
                    
    let map (dataFn:'DataIn->'DataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) =
        mapData dataFn >> mapIndex indexCodec