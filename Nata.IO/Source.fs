namespace Nata.IO

type Source<'Channel,'Data,'Index> = 'Channel -> List<Capability<'Data,'Index>>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Source =

    let mapChannel (codec:Codec<'ChannelIn,'ChannelOut>)
                   (source:Source<'ChannelIn,'Data,'Index>) : Source<'ChannelOut,'Data,'Index> =
        Codec.decoder codec >> source

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (source:Source<'Channel,'DataIn,'Index>) : Source<'Channel,'DataOut,'Index> =
        source >> List.map (Capability.mapData codec)

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (source:Source<'Channel,'Data,'IndexIn>) : Source<'Channel,'Data,'IndexOut> =
        source >> List.map (Capability.mapIndex codec)

    let map (channelCodec:Codec<'ChannelIn,'ChannelOut>)
            (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (source:Source<'ChannelIn,'DataIn,'IndexIn>) : Source<'ChannelOut,'DataOut,'IndexOut> =
        Codec.decoder channelCodec >> source
        >> List.map (Capability.mapData dataCodec)
        >> List.map (Capability.mapIndex indexCodec)