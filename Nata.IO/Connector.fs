namespace Nata.IO

type Connector<'Configuration,'Channel,'Data,'Index> = 'Configuration -> Source<'Channel,'Data,'Index>
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Connector =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (connector:Connector<'Configuration,'Channel,'DataIn,'Index>) : Connector<'Configuration,'Channel,'DataOut,'Index> =
        connector >> Source.mapData codec

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (connector:Connector<'Configuration,'Channel,'Data,'IndexIn>) : Connector<'Configuration,'Channel,'Data,'IndexOut> =
        connector >> Source.mapIndex codec

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (connector:Connector<'Configuration,'Channel,'DataIn,'IndexIn>) : Connector<'Configuration,'Channel,'DataOut,'IndexOut> =
        connector >> Source.map dataCodec indexCodec