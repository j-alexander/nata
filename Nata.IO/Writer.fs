namespace Nata.IO

type Writer<'Data> = Event<'Data> -> unit

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Writer =

    let mapData (fn:'DataIn->'DataOut)
                (writer:Writer<'DataOut>) : Writer<'DataIn> =
        Event.mapData fn >> writer

    let map = mapData