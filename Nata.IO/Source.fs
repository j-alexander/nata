namespace Nata.IO

open Nata.Core

type Source<'Channel,'Data,'Index> = 'Channel -> Channel<'Data,'Index>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Source =

    let mapCapabilities (fn:Capability<'Data,'Index>->Capability<'Data,'Index>)
                        (source:Source<'Channel,'Data,'Index>) : Source<'Channel,'Data,'Index> =
        source >> Channel.mapCapabilities fn

    let mapChannel (codec:Codec<'ChannelIn,'ChannelOut>)
                   (source:Source<'ChannelIn,'Data,'Index>) : Source<'ChannelOut,'Data,'Index> =
        Codec.decoder codec >> source

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (source:Source<'Channel,'DataIn,'Index>) : Source<'Channel,'DataOut,'Index> =
        source >> Channel.mapData codec

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (source:Source<'Channel,'Data,'IndexIn>) : Source<'Channel,'Data,'IndexOut> =
        source >> Channel.mapIndex codec

    let map (channelCodec:Codec<'ChannelIn,'ChannelOut>)
            (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (source:Source<'ChannelIn,'DataIn,'IndexIn>) : Source<'ChannelOut,'DataOut,'IndexOut> =
        Codec.decoder channelCodec >> source
        >> Channel.map dataCodec indexCodec