namespace Nata.IO

type WriterTo<'Data,'Index> = 'Index -> Event<'Data> -> 'Index

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WriterTo =

    let mapData (fn:'DataIn->'DataOut)
                (writerTo:WriterTo<'DataOut,'Index>) : WriterTo<'DataIn,'Index> =
        fun index ->
            Event.mapData fn >> writerTo index

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (writerTo:WriterTo<'Data,'IndexOut>) : WriterTo<'Data,'IndexIn> =
        fun index ->
            InvalidPosition.applyMap decode (writerTo (encode index) >> decode)
                    
    let map (dataFn:'DataIn->'DataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) =
        mapData dataFn >> mapIndex indexCodec