namespace Nata.IO

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