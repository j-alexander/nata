namespace Nata.IO

type SubscriberFrom<'Data,'Metadata,'Index> = 'Index -> seq<Event<'Data,'Metadata> * 'Index>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SubscriberFrom =

    let mapData (fn:'DataIn->'DataOut)
                (subscriberFrom:SubscriberFrom<'DataIn,'Metadata,'Index>) : SubscriberFrom<'DataOut,'Metadata,'Index> =
        subscriberFrom >> Seq.mapFst (Event.mapData fn)

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (subscriberFrom:SubscriberFrom<'Data,'MetadataIn,'Index>) : SubscriberFrom<'Data,'MetadataOut,'Index> =
        subscriberFrom >> Seq.mapFst (Event.mapMetadata fn)

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (subscriberFrom:SubscriberFrom<'Data,'Metadata,'IndexIn>) : SubscriberFrom<'Data,'Metadata,'IndexOut> =
        decode >> subscriberFrom >> Seq.mapSnd encode

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (indexCodec:Codec<'IndexIn,'IndexOut>) = 
        mapData dataFn >> mapMetadata metadataFn >> mapIndex indexCodec