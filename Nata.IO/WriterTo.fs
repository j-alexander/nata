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

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (writerTo:WriterTo<'DataOut,'MetadataOut,'Index>) : WriterTo<'DataIn,'MetadataIn,'Index> = 
        fun index ->
            Event.mapData dataFn >> Event.mapMetadata metadataFn >> writerTo index