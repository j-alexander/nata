namespace Nata.Service

open Nata.IO

type Merge<'Input,'Output> = 'Output option -> 'Input -> 'Output
type MergeEvent<'Input,'Output> = Event<'Output> option -> Event<'Input> -> 'Output

module Merge =
    let usingInput : Merge<'Input,'Input> = fun _ x -> x
    let using value : Merge<'Input,'Output> = fun _ _ -> value

module MergeEvent =
    let usingInput : MergeEvent<'Input,'Input> = fun _ { Event.Data=x } -> x
    let using value : MergeEvent<'Input,'Output> = fun _ _ -> value
