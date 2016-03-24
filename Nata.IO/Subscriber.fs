namespace Nata.IO

type Subscriber<'Data> = unit -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Subscriber =

    let mapData (fn:'DataIn->'DataOut)
                (subscriber:Subscriber<'DataIn>) : Subscriber<'DataOut> =
        subscriber >> Seq.map (Event.mapData fn)

    let map = mapData