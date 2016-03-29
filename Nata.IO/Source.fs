namespace Nata.IO

type Source<'Channel,'Data,'Index> = 'Channel -> List<Capability<'Data,'Index>>
    

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Source =

    let mapData (codec:Codec<'DataIn,'DataOut>)
                (source:Source<'Channel,'DataIn,'Index>) : Source<'Channel,'DataOut,'Index> =
        source >> List.map (Capability.mapData codec)

    let mapIndex (codec:Codec<'IndexIn,'IndexOut>)
                 (source:Source<'Channel,'Data,'IndexIn>) : Source<'Channel,'Data,'IndexOut> =
        source >> List.map (Capability.mapIndex codec)

    let map (dataCodec:Codec<'DataIn,'DataOut>)
            (indexCodec:Codec<'IndexIn,'IndexOut>)
            (source:Source<'Channel,'DataIn,'IndexIn>) : Source<'Channel,'DataOut,'IndexOut> =
        source >> List.map (Capability.mapData dataCodec) >> List.map (Capability.mapIndex indexCodec)