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

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (subscriberFrom:SubscriberFrom<'DataIn,'MetadataIn,'Index>) : SubscriberFrom<'DataOut,'MetadataOut,'Index> = 
        subscriberFrom >> Seq.mapFst (Event.mapData dataFn >> Event.mapMetadata metadataFn)