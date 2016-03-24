namespace Nata.IO

type Reader<'Data> = unit -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Reader =

    let mapData (fn:'DataIn->'DataOut)
                (reader:Reader<'DataIn>) : Reader<'DataOut> =
        reader >> Seq.map (Event.mapData fn)

    let map = mapData