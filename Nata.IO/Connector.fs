namespace Nata.IO

open Nata.Core

type Connector<'Configuration,'Channel,'Data,'Index> = 'Configuration -> Source<'Channel,'Data,'Index>
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Connector =

    let mapConfiguration (codec:Codec<'ConfigurationIn,'ConfigurationOut>)
                         (connector:Connector<'ConfigurationIn,'Channel,'Data,'Index>) : Connector<'ConfigurationOut,'Channel,'Data,'Index> =
        Codec.decoder codec >> connector

    let mapChannel (codec:Codec<'ChannelIn,'ChannelOut>)
                   (connector:Connector<'Configuration,'ChannelIn,'Data,'Index>) : Connector<'Configuration,'ChannelOut,'Data,'Index> =
        connector >> Source.mapChannel codec

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (connector:Connector<'Configuration,'Channel,'DataIn,'Index>) : Connector<'Configuration,'Channel,'DataOut,'Index> =
        connector >> Source.mapData codec

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (connector:Connector<'Configuration,'Channel,'Data,'IndexIn>) : Connector<'Configuration,'Channel,'Data,'IndexOut> =
        connector >> Source.mapIndex codec

    let map (configurationCodec:Codec<'ConfigurationIn,'ConfigurationOut>)
            (channelCodec:Codec<'ChannelIn,'ChannelOut>)
            (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (connector:Connector<'ConfigurationIn,'ChannelIn,'DataIn,'IndexIn>) : Connector<'ConfigurationOut,'ChannelOut,'DataOut,'IndexOut> =
        Codec.decoder configurationCodec >> connector
        >> Source.map channelCodec dataCodec indexCodec