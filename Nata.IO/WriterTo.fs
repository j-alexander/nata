namespace Nata.IO

type WriterTo<'Data,'Metadata,'Index> = 'Index -> Event<'Data,'Metadata> -> 'Index

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WriterTo =

    let mapData (fn:'DataIn->'DataOut)
                (writerTo:WriterTo<'DataOut,'Metadata,'Index>) : WriterTo<'DataIn,'Metadata,'Index> =
        fun index ->
            Event.mapData fn >> writerTo index

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (writerTo:WriterTo<'Data,'MetadataOut,'Index>) : WriterTo<'Data,'MetadataIn,'Index> =
        fun index ->
            Event.mapMetadata fn >> writerTo index

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (writerTo:WriterTo<'Data,'Metadata,'IndexOut>) : WriterTo<'Data,'Metadata,'IndexIn> =
        fun index ->
            InvalidPosition.applyMap decode (writerTo (encode index) >> decode)
                    

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) =
        mapData dataFn >> mapMetadata metadataFn >> mapIndex indexCodec