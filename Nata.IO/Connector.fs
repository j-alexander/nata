namespace Nata.IO

type Connector<'Configuration,'Channel,'Data,'Index> = 'Configuration -> Source<'Channel,'Data,'Index>
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Connector =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (connector:Connector<'Configuration,'Channel,'DataOut,'Index>) : Connector<'Configuration,'Channel,'DataIn,'Index> =
        connector >> Source.mapData codec

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (connector:Connector<'Configuration,'Channel,'Data,'IndexOut>) : Connector<'Configuration,'Channel,'Data,'IndexIn> =
        connector >> Source.mapIndex codec

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (connector:Connector<'Configuration,'Channel,'DataOut,'IndexOut>) : Connector<'Configuration,'Channel,'DataIn,'IndexIn> =
        connector >> Source.map dataCodec indexCodec