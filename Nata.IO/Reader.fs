namespace Nata.IO

type Reader<'Data> = unit -> seq<Event<'Data>>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Reader =

    let mapData (fn:'DataIn->'DataOut)
                (reader:Reader<'DataIn>) : Reader<'DataOut> =
        reader >> Seq.map (Event.mapData fn)

    let map = mapData

    let filterData (fn:'Data->bool)
                   (reader:Reader<'Data>) : Reader<'Data> =
        reader >> Seq.filter (Event.data >> fn)
        
    let filter = filterData

    let chooseData (fn:'DataIn->'DataOut option)
                   (reader:Reader<'DataIn>) : Reader<'DataOut> =
        reader >> Seq.choose (Event.chooseData fn)

    let choose = chooseData