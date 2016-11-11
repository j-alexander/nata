namespace Nata.IO

open Nata.Core

type SubscriberFrom<'Data,'Index> = Position<'Index> -> seq<Event<'Data> * 'Index>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SubscriberFrom =

    let mapData (fn:'DataIn->'DataOut)
                (subscriberFrom:SubscriberFrom<'DataIn,'Index>) : SubscriberFrom<'DataOut,'Index> =
        subscriberFrom >> Seq.mapFst (Event.mapData fn)

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (subscriberFrom:SubscriberFrom<'Data,'IndexIn>) : SubscriberFrom<'Data,'IndexOut> =
        Position.map decode >>
            Position.applyMap encode (subscriberFrom >> Seq.mapSnd encode)

    let map (dataFn:'DataIn->'DataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) = 
        mapData dataFn >> mapIndex indexCodec
        
    let filterData (fn:'Data->bool)
                   (subscriberFrom:SubscriberFrom<'Data,'Index>) : SubscriberFrom<'Data,'Index> = 
        subscriberFrom >> Seq.filter (fst >> Event.data >> fn)

    let filterIndex (fn:'Index->bool)
                    (subscriberFrom:SubscriberFrom<'Data,'Index>) : SubscriberFrom<'Data,'Index> = 
        subscriberFrom >> Seq.filter (snd >> fn)
                    
    let filter (dataFn:'Data->bool)
               (indexFn:'Index->bool) =
        filterData dataFn >> filterIndex indexFn

    let chooseData (fn:'DataIn->'DataOut option)
                   (subscriberFrom:SubscriberFrom<'DataIn,'Index>) : SubscriberFrom<'DataOut,'Index> =
        subscriberFrom >> Seq.chooseFst (Event.chooseData fn)

    let chooseIndex (fn:'Index->'Index option)
                    (subscriberFrom:SubscriberFrom<'Data,'Index>) : SubscriberFrom<'Data,'Index> =
        subscriberFrom >> Seq.chooseSnd fn
                    
    let choose (dataFn:'DataIn->'DataOut option)
               (indexFn:'Index->'Index option) =
        chooseData dataFn >> chooseIndex indexFn
