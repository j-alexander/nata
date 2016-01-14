namespace Nata.IO

type Source<'Channel,'Data,'Metadata,'Index> = 'Channel -> List<Capability<'Data,'Metadata,'Index>>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Source =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (source:Source<'Channel,'DataOut,'Metadata,'Index>) : Source<'Channel,'DataIn,'Metadata,'Index> =
        source >> List.map (Capability.mapData codec)

    let mapMetadata (codec:Codec<'MetadataIn,'MetadataOut>)
                    (source:Source<'Channel,'Data,'MetadataOut,'Index>) : Source<'Channel,'Data,'MetadataIn,'Index> =
        source >> List.map (Capability.mapMetadata codec)

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (metadataCodec:Codec<'MetadataIn,'MetadataOut>)
            (source:Source<'Channel,'DataOut,'MetadataOut,'Index>) : Source<'Channel,'DataIn,'MetadataIn,'Index> =
        source >> List.map (Capability.mapData dataCodec >> Capability.mapMetadata metadataCodec)