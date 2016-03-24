namespace Nata.IO

type Source<'Channel,'Data,'Index> = 'Channel -> List<Capability<'Data,'Index>>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Source =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (source:Source<'Channel,'DataOut,'Index>) : Source<'Channel,'DataIn,'Index> =
        source >> List.map (Capability.mapData codec)

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (source:Source<'Channel,'Data,'IndexOut>) : Source<'Channel,'Data,'IndexIn> =
        source >> List.map (Capability.mapIndex codec)

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (source:Source<'Channel,'DataOut,'IndexOut>) : Source<'Channel,'DataIn,'IndexIn> =
        source >> List.map (Capability.mapData dataCodec) >> List.map (Capability.mapIndex indexCodec)