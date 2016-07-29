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
        
    let filterData (fn:'Data->bool)
                   (readerFrom:ReaderFrom<'Data,'Index>) : ReaderFrom<'Data,'Index> = 
        readerFrom >> Seq.filter (fst >> Event.data >> fn)

    let filterIndex (fn:'Index->bool)
                    (readerFrom:ReaderFrom<'Data,'Index>) : ReaderFrom<'Data,'Index> = 
        readerFrom >> Seq.filter (snd >> fn)
                    
    let filter (dataFn:'Data->bool)
               (indexFn:'Index->bool) =
        filterData dataFn >> filterIndex indexFn

    let chooseData (fn:'DataIn->'DataOut option)
                   (readerFrom:ReaderFrom<'DataIn,'Index>) : ReaderFrom<'DataOut,'Index> =
        readerFrom >> Seq.chooseFst (Event.chooseData fn)

    let chooseIndex (fn:'Index->'Index option)
                    (readerFrom:ReaderFrom<'Data,'Index>) : ReaderFrom<'Data,'Index> =
        readerFrom >> Seq.chooseSnd fn
                    
    let choose (dataFn:'DataIn->'DataOut option)
               (indexFn:'Index->'Index option) =
        chooseData dataFn >> chooseIndex indexFn

