namespace Nata.IO

type Reader<'Data,'Metadata> = unit -> seq<Event<'Data,'Metadata>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Reader =

    let mapData (fn:'DataIn->'DataOut)
                (reader:Reader<'DataIn,'Metadata>) : Reader<'DataOut,'Metadata> =
        reader >> Seq.map (Event.mapData fn)

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (reader:Reader<'Data,'MetadataIn>) : Reader<'Data,'MetadataOut> =
        reader >> Seq.map (Event.mapMetadata fn)

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (reader:Reader<'DataIn,'MetadataIn>) : Reader<'DataOut,'MetadataOut> = 
        reader >> Seq.map (Event.mapData dataFn >> Event.mapMetadata metadataFn)