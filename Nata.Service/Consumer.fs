namespace Nata.Service

open System
open Nata.Core
open Nata.IO

type Consumer<'StateOrAccumulator,'InputIndex> = {
    State:'StateOrAccumulator
    Index:'InputIndex
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Consumer =

    // field accessors
    let state { State=x } = x
    let inputIndex { Index=x } = x

    // starts a consumer on the current thread
    let start (output:seq<'a>) =
        Seq.iter ignore output

    // starts a consumer asynchronously
    let startAsync (output:seq<'a>) =
        Async.Start <| async { Seq.iter ignore output }

    // reset the input checkpoint index and current state
    let reset (write:Writer<Consumer<'State,'Index>>)
              (index:'Index)
              (state:'State) =
        { Consumer.Index = index
          Consumer.State = state }
        |> Event.create
        |> write

    // compete for input events using the fold pattern
    let foldEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                  (compete:Competitor<Consumer<'State,'InputIndex>>)
                  (fn:'State option->Event<'Input>->'State) =
        compete <| fun last ->
            let state, index =
                last
                |> Option.map (function { Event.Data={ State=state; Index=index }} -> state, index)
                |> Option.distribute
            let input, index =
                match index with
                | Some i -> Position.After(Position.At i)
                | None -> Position.Start
                |> subscribeFrom
                |> Seq.head
            { Consumer.Index = index
              Consumer.State = fn state input }
            |> Event.create

    // compete for inputs using the fold pattern
    let fold (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
             (compete:Competitor<Consumer<'State,'InputIndex>>)
             (fn:'State option->'Input->'State) =
        fun s -> Event.data >> fn s
        |> foldEvent subscribeFrom compete
        |> Seq.map Event.data

    // compete for input events and apply a projection
    let mapEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                 (compete:Competitor<Consumer<'Output,'InputIndex>>)
                 (fn:Event<'Input>->'Output) =
        fun _ -> fn
        |> foldEvent subscribeFrom compete

    // compete for inputs and apply a projection
    let map (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
            (compete:Competitor<Consumer<'Output,'InputIndex>>)
            (fn:'Input->'Output) =
        fun _ -> fn
        |> fold subscribeFrom compete

    // compete to fold over two types of input events
    let bifoldEvent (l:SubscriberFrom<'L,'IndexL>)
                    (r:SubscriberFrom<'R,'IndexR>)
                    (compete:Competitor<Consumer<'State,'IndexL option*'IndexR option>>)
                    (fn:'State option->Choice<Event<'L>,Event<'R>>->'State) =
        compete <| fun last ->
            let state, (indexL, indexR) =
                last
                |> Option.map (function { Event.Data={ State=state; Index=index }} -> state, index)
                |> Option.distribute
                |> mapSnd (Option.distribute >> mapFst Option.join >> mapSnd Option.join)
            let positionFor index =
                index
                |> Option.map (Position.At >> Position.After)
                |> Option.defaultValue Position.Start
            let input, index =
                [ (l (positionFor indexL)) |> Seq.mapFst Choice1Of2 |> Seq.mapSnd Choice1Of2
                  (r (positionFor indexR)) |> Seq.mapFst Choice2Of2 |> Seq.mapSnd Choice2Of2 ]
                |> Seq.consume
                |> Seq.head
                |> mapSnd (function | Choice1Of2 l -> Some l, indexR
                                    | Choice2Of2 r -> indexL, Some r)
            { Consumer.Index = index
              Consumer.State = fn state input }
            |> Event.create

    // compete to fold over two types of input
    let bifold (l:SubscriberFrom<'L,'IndexL>)
               (r:SubscriberFrom<'R,'IndexR>)
               (compete:Competitor<Consumer<'State,'IndexL option*'IndexR option>>)
               (fn:'State option->Choice<'L,'R>->'State) =
        fun s -> function
        | Choice1Of2 e -> e |> Event.data |> Choice1Of2 |> fn s
        | Choice2Of2 e -> e |> Event.data |> Choice2Of2 |> fn s
        |> bifoldEvent l r compete
        |> Seq.map Event.data

    // compete for two types of input events and apply a projection
    let bimapEvent (l:SubscriberFrom<'L,'IndexL>)
                   (r:SubscriberFrom<'R,'IndexR>)
                   (compete:Competitor<Consumer<'State,'IndexL option*'IndexR option>>)
                   (fn:Choice<Event<'L>,Event<'R>>->'State) =
        fun _ -> fn
        |> bifoldEvent l r compete

    // compete for two types of inputs and apply a projection
    let bimap (l:SubscriberFrom<'L,'IndexL>)
              (r:SubscriberFrom<'R,'IndexR>)
              (compete:Competitor<Consumer<'State,'IndexL option*'IndexR option>>)
              (fn:Choice<'L,'R>->'State) =
        fun _ -> fn
        |> bifold l r compete

    // compete to fold over many input events of the same type
    let multifoldEvent (subscribersByCheckpoint:Map<'SourceId, SubscriberFrom<'Input,'InputIndex>>)
                       (compete:Competitor<Consumer<'State,Map<'SourceId,'InputIndex>>>)
                       (fn:'State option->Event<'Input>->'State) =
        compete <| fun last ->
            let state, indexes =
                last
                |> Option.map (function { Event.Data={ State=state; Index=index }} -> state, index)
                |> Option.distribute
                |> mapSnd (Option.defaultValue Map.empty)
            let positionFor name =
                indexes
                |> Map.tryFind name
                |> Option.map (Position.At >> Position.After)
                |> Option.defaultValue Position.Start
            let name,index,input =
                subscribersByCheckpoint
                |> Map.toList
                |> List.map(fun (name, subscribeFrom) ->
                    subscribeFrom (positionFor name)
                    |> Seq.map (fun (input,index) -> name,index,input))
                |> Seq.consume
                |> Seq.head
            { Consumer.Index = Map.add name index indexes
              Consumer.State = fn state input }
            |> Event.create

    // compete to fold over many inputs of the same type
    let multifold (subscribersByCheckpoint:Map<'SourceId, SubscriberFrom<'Input,'InputIndex>>)
                  (compete:Competitor<Consumer<'State,Map<'SourceId,'InputIndex>>>)
                  (fn:'State option->'Input->'State) =
        fun s -> Event.data >> fn s
        |> multifoldEvent subscribersByCheckpoint compete
        |> Seq.map Event.data

    // compete for many input events of the same type and apply a projection
    let multimapEvent (subscribersByCheckpoint:Map<'SourceId, SubscriberFrom<'Input,'InputIndex>>)
                      (compete:Competitor<Consumer<'State,Map<'SourceId,'InputIndex>>>)
                      (fn:Event<'Input>->'State) =
        fun _ -> fn
        |> multifoldEvent subscribersByCheckpoint compete

    // compete for many inputs of the same type and apply a projection
    let multimap (subscribersByCheckpoint:Map<'SourceId, SubscriberFrom<'Input,'InputIndex>>)
                 (compete:Competitor<Consumer<'State,Map<'SourceId,'InputIndex>>>)
                 (fn:'Input->'State) =
        fun _ -> fn
        |> multifold subscribersByCheckpoint compete

    // compete for input events with:
    // - a shared checkpoint among competitors, and
    // - an event handler with at-least-once call semantics across competitors
    let consumeEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                     (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                     (handle:(Event<'Input>*'InputIndex)->unit) =
        checkpoint <| fun last ->
            let input, index =
                match last with
                | Some { Event.Data={ Index=i }} -> Position.After(Position.At i)
                | None -> Position.Start
                |> subscribeFrom
                |> Seq.head

            do handle(input, index)

            { Consumer.Index = index
              Consumer.State = () }
            |> Event.create

    // compete for input events with:
    // - a shared checkpoint among competitors, and
    // - an event handler with at-least-once call semantics across competitors
    let consume (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                (handle:('Input*'InputIndex)->unit) =
        mapFst Event.data >> handle
        |> consumeEvent subscribeFrom checkpoint
        |> Seq.map Event.data

    // produce only if the value being written is newer (by input index) than the existing data
    let produce ((readFrom, writeTo):(ReaderFrom<Consumer<'Input,'InputIndex>,'OutputIndex>
                                      * WriterTo<Consumer<'Input,'InputIndex>,'OutputIndex>))
                ((input, index):('Input*'InputIndex)) =
        let writeTo position =
            try { Consumer.State=input
                  Consumer.Index=index }
                |> Event.create
                |> writeTo position
                |> ignore
            with :? Position.Invalid<'OutputIndex> -> ()

        try readFrom (Position.Before Position.End)
            |> Seq.mapFst (Event.data >> inputIndex)
            |> Seq.tryHead
        with :? ArgumentOutOfRangeException -> None
        |>
        function
        | Some (i, _) when (i >= index) -> ()
        | Some (i, o) ->
            Position.At o
            |> Position.After
            |> writeTo
        | _ ->
            Position.Start
            |> writeTo

    // produce only if the value being written is newer (by input index) than the existing data
    let produceEvent ((readFrom, writeTo):(ReaderFrom<Consumer<'Input,'InputIndex>,'OutputIndex>
                                           * WriterTo<Consumer<'Input,'InputIndex>,'OutputIndex>))
                     ((input, index):(Event<'Input>*'InputIndex)) =
        produce (readFrom, writeTo) (Event.data input, index)

    // produce only if the value being written is newer (by input index) than the existing data
    // for the source id supplied
    let multiproduce ((readFrom, writeTo):(ReaderFrom<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                          * WriterTo<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>))
                     (id:'SourceId)
                     ((input, index):('Input*'InputIndex)) =
        let writeTo map position =
            try { Consumer.State=input
                  Consumer.Index=Map.add id index map }
                |> Event.create
                |> writeTo position
                |> ignore
            with :? Position.Invalid<'OutputIndex> -> ()

        try readFrom (Position.Before Position.End)
            |> Seq.mapFst (Event.data >> inputIndex)
            |> Seq.tryHead
        with :? ArgumentOutOfRangeException -> None
        |>
        function
        | Some (map, _) when (Map.containsKey id map &&
                              Map.find id map >= index) -> ()
        | Some (map, o) ->
            Position.At o
            |> Position.After
            |> writeTo map
        | _ ->
            Position.Start
            |> writeTo Map.empty

    // produce only if the value being written is newer (by input index) than the existing data
    // for the source id supplied
    let multiproduceEvent ((readFrom, writeTo):(ReaderFrom<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                * WriterTo<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>))
                          (id:'SourceId)
                          ((input, index):(Event<'Input>*'InputIndex)) =
        multiproduce (readFrom, writeTo) id (Event.data input, index)

    // compete to distribute input events
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let distributeEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                        (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                        (distribute:Event<'Input>->List<ReaderFrom<Consumer<'Output,'InputIndex>,'OutputIndex>
                                                        * WriterTo<Consumer<'Output,'InputIndex>,'OutputIndex>
                                                        * Event<'Output>>) =
        consumeEvent subscribeFrom checkpoint <| fun (input, index) ->
            for (readerFrom, writerTo, output) in distribute input do
                produceEvent (readerFrom, writerTo) (output, index)

    // compete to distribute inputs
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let distribute (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                   (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                   (distribute:'Input->List<ReaderFrom<Consumer<'Output,'InputIndex>,'OutputIndex>
                                            * WriterTo<Consumer<'Output,'InputIndex>,'OutputIndex>
                                            * 'Output>) =
        distributeEvent subscribeFrom checkpoint <| fun input ->
            distribute (Event.data input)
            |> List.map (fun (r,w,o) -> r,w,Event.create o)
        |> Seq.map Event.data

    // compete to partition input events
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let partitionEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                       (checkpoint:Competitor<Consumer<unit,'InputIndex>>) 
                       (partition:Event<'Input>->(ReaderFrom<Consumer<'Input,'InputIndex>,'OutputIndex>
                                                  * WriterTo<Consumer<'Input,'InputIndex>,'OutputIndex>)) =
        distributeEvent subscribeFrom checkpoint <| fun input ->
            let readerFrom, writerTo = partition input
            [ readerFrom, writerTo, input ]

    // compete to partition inputs
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let partition (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                  (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                  (partition:'Input->(ReaderFrom<Consumer<'Input,'InputIndex>,'OutputIndex>
                                      * WriterTo<Consumer<'Input,'InputIndex>,'OutputIndex>)) =
        distribute subscribeFrom checkpoint <| fun input ->
            let readerFrom, writerTo = partition input
            [ readerFrom, writerTo, input ]

    // compete to distribute input events
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index for the source id supplied
    //
    let multidistributeEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                             (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                             (id:'SourceId)
                             (distribute:Event<'Input>->List<ReaderFrom<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                             * WriterTo<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                             * Event<'Output>>) =
        consumeEvent subscribeFrom checkpoint <| fun (input, index) ->
            for (readerFrom, writerTo, output) in distribute input do
                multiproduceEvent (readerFrom, writerTo) id (output, index)

    // compete to distribute inputs
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let multidistribute (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                        (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                        (id:'SourceId)
                        (distribute:'Input->List<ReaderFrom<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                 * WriterTo<Consumer<'Output,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                 * 'Output>) =
        multidistributeEvent subscribeFrom checkpoint id <| fun input ->
            distribute (Event.data input)
            |> List.map (fun (r,w,o) -> r,w,Event.create o)
        |> Seq.map Event.data

    // compete to partition input events
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let multipartitionEvent (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                            (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                            (id:'SourceId)
                            (partition:Event<'Input>->(ReaderFrom<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                                       * WriterTo<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>)) =
        multidistributeEvent subscribeFrom checkpoint id <| fun input ->
            let readerFrom, writerTo = partition input
            [ readerFrom, writerTo, input ]

    // compete to partition inputs
    //
    // output consumers are _only_ written if their input index is less than the current
    // subscription index
    //
    // -> therefore all data on the published outputs must originate from this input
    //
    let multipartition (subscribeFrom:SubscriberFrom<'Input,'InputIndex>)
                       (checkpoint:Competitor<Consumer<unit,'InputIndex>>)
                       (id:'SourceId)
                       (partition:'Input->(ReaderFrom<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>
                                           * WriterTo<Consumer<'Input,Map<'SourceId,'InputIndex>>,'OutputIndex>)) =
        multidistribute subscribeFrom checkpoint id <| fun input ->
            let readerFrom, writerTo = partition input
            [ readerFrom, writerTo, input ]