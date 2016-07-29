namespace Nata.IO

type Subscriber<'Data> = unit -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Subscriber =

    let mapData (fn:'DataIn->'DataOut)
                (subscriber:Subscriber<'DataIn>) : Subscriber<'DataOut> =
        subscriber >> Seq.map (Event.mapData fn)

    let map = mapData

    let filterData (fn:'Data->bool)
                   (subscriber:Subscriber<'Data>) : Subscriber<'Data> =
        subscriber >> Seq.filter (Event.data >> fn)
        
    let filter = filterData

    let chooseData (fn:'DataIn->'DataOut option)
                   (subscriber:Subscriber<'DataIn>) : Subscriber<'DataOut> =
        subscriber >> Seq.choose (Event.chooseData fn)

    let choose = chooseData
