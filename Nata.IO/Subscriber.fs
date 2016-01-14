namespace Nata.IO

type Subscriber<'Data,'Metadata> = unit -> seq<Event<'Data,'Metadata>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Subscriber =

    let mapData (fn:'DataIn->'DataOut)
                (subscriber:Subscriber<'DataIn,'Metadata>) : Subscriber<'DataOut,'Metadata> =
        subscriber >> Seq.map (Event.mapData fn)

    let mapMetadata (fn:'MetadataIn->'MetadataOut)
                    (subscriber:Subscriber<'Data,'MetadataIn>) : Subscriber<'Data,'MetadataOut> =
        subscriber >> Seq.map (Event.mapMetadata fn)

    let map (dataFn:'DataIn->'DataOut)
            (metadataFn:'MetadataIn->'MetadataOut)
            (subscriber:Subscriber<'DataIn,'MetadataIn>) : Subscriber<'DataOut,'MetadataOut> = 
        subscriber >> Seq.map (Event.mapData dataFn >> Event.mapMetadata metadataFn)