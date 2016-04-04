namespace Nata.IO

type ReaderFrom<'Data,'Index> = Position<'Index> -> seq<Event<'Data> * 'Index>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReaderFrom =

    let mapData (fn:'DataIn->'DataOut)
                (readerFrom:ReaderFrom<'DataIn,'Index>) : ReaderFrom<'DataOut,'Index> =
        readerFrom >> Seq.mapFst (Event.mapData fn)

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (readerFrom:ReaderFrom<'Data,'IndexIn>) : ReaderFrom<'Data,'IndexOut> =
        Position.map decode >>
            Position.applyMap encode (readerFrom >> Seq.mapSnd encode)

    let map (dataFn:'DataIn->'DataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) = 
        mapData dataFn >> mapIndex indexCodec