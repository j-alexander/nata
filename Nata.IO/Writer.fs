namespace Nata.IO

type Writer<'Data,'Metadata> = Event<'Data,'Metadata> -> unit

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Writer =

    let mapData (fn:'DataIn->'DataOut)
                (writer:Writer<'DataOut,'Metadata>) : Writer<'DataIn,'Metadata> =
        Event.mapData fn >> writer

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (writer:Writer<'Data,'MetadataOut>) : Writer<'Data,'MetadataIn> =
        Event.mapMetadata fn >> writer

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (writer:Writer<'DataOut,'MetadataOut>) : Writer<'DataIn,'MetadataIn> = 
        Event.mapData dataFn >> Event.mapMetadata metadataFn >> writer