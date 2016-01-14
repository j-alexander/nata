namespace Nata.IO

type ReaderFrom<'Data,'Metadata,'Index> = 'Index -> seq<Event<'Data,'Metadata> * 'Index>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReaderFrom =

    let mapData (fn:'DataIn->'DataOut)
                (readerFrom:ReaderFrom<'DataIn,'Metadata,'Index>) : ReaderFrom<'DataOut,'Metadata,'Index> =
        readerFrom >> Seq.mapFst (Event.mapData fn)

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (readerFrom:ReaderFrom<'Data,'MetadataIn,'Index>) : ReaderFrom<'Data,'MetadataOut,'Index> =
        readerFrom >> Seq.mapFst (Event.mapMetadata fn)

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (readerFrom:ReaderFrom<'DataIn,'MetadataIn,'Index>) : ReaderFrom<'DataOut,'MetadataOut,'Index> = 
        readerFrom >> Seq.mapFst (Event.mapData dataFn >> Event.mapMetadata metadataFn)