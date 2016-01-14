namespace Nata.IO

type Connector<'Configuration,'Channel,'Data,'Metadata,'Index> = 'Configuration -> Source<'Channel,'Data,'Metadata,'Index>
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Connector =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (connector:Connector<'Configuration,'Channel,'DataOut,'Metadata,'Index>) : Connector<'Configuration,'Channel,'DataIn,'Metadata,'Index> =
        connector >> Source.mapData codec

    let mapMetadata (codec:Codec<'MetadataIn,'MetadataOut>)
                    (connector:Connector<'Configuration,'Channel,'Data,'MetadataOut,'Index>) : Connector<'Configuration,'Channel,'Data,'MetadataIn,'Index> =
        connector >> Source.mapMetadata codec

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (metadataCodec:Codec<'MetadataIn,'MetadataOut>)
            (connector:Connector<'Configuration,'Channel,'DataOut,'MetadataOut,'Index>) : Connector<'Configuration,'Channel,'DataIn,'MetadataIn,'Index> =
        connector >> Source.map dataCodec metadataCodec