namespace API

open System
open FSharp.Data


type Message =
    | Write of Write
    | Listen of Listen
    | Stop


type Node = MailboxProcessor<Message>



[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Node =

    type private State = {
        value : Value
        children : Map<string, Node>
        listeners : Listeners
    }


    let private notify (state:State) =
        let listeners = Listeners.get state.listeners
        for listener in listeners do
            listener.Post(state.value)


    let private listen (listen:Listen) (state:State) =
        match listen.path with
        | [] ->
            // notify the listener of the current state
            listen.listener.Post(state.value)
            // listen to changes at this node
            { state with
                listeners = Listeners.add listen state.listeners }
        | child :: path ->
            // reduce the path relative to the child
            let listen = { listen with path = path }

            match state.children |> Map.tryFind child with
            // pass the listen request to the child node
            | Some node -> listen |> Message.Listen |> node.Post
            // currently no such path exists, wait until one does
            | None -> () 
            // track the listener for future activity at that path
            { state with
                listeners = Listeners.addChild child listen state.listeners }


    let private attach (name:string) (state:State) =
        let node = state.children.[name]
        state.listeners |> Listeners.getChild name |> Seq.iter(Listen >> node.Post)
        state


    let private clear (state:State) =
        for (key,node) in state.children |> Map.toSeq do
            node.Post(Stop)
        { state with
            value = Value.empty 
            children = Map.empty}


    let private write (json:JsonValue) (tick:int64) (state:State) =
        { state with
            value = Value.create json tick }


    let private fromJsonArray =
        Seq.mapi (fun i (json:JsonValue) -> i.ToString(), json) >> Seq.toList


    let private fromJsonRecord =
        Seq.map (fun (key:string,json:JsonValue) -> key, json) >> Seq.toList

    let private join (existing:List<string*JsonValue>)
                     (incoming:List<string*JsonValue>) =
        [ yield!
            query {
                let incoming = incoming |> Seq.map fst |> Set.ofSeq
                for existing in existing do
                where (incoming |> Set.contains (fst existing) |> not)
                select (Some existing, None)
            }
          yield!
            query {
                let existing = existing |> Seq.map fst |> Set.ofSeq
                for incoming in incoming do
                where (existing |> Set.contains (fst incoming) |> not)
                select (None, Some incoming)
            }
          yield!
            query {
                for existing in existing do
                for incoming in incoming do
                where ((fst existing) = (fst incoming))
                select (Some existing, Some incoming)
            }
        ]



    // create a new node with the specified name
    let rec create (name:string) : Node =

        let createChild (tick:int64) (state:State) (key, json)=
            // create the child and update the state's children
            let child = create key
            let children = Map.add key child state.children

            { path = []
              writer = None
              tick = tick
              json = json } |> Write |> child.Post

            // attach listeners to the node
            { state with children = children } |> attach key


        let removeChild (state:State) (key:string) : State =
            // stop the child
            match state.children |> Map.tryFind key with
            | None -> () | Some child -> Stop |> child.Post
             
            // remove the child
            { state with children = state.children |> Map.remove key }


        let mergeChild (tick:int64) (state:State) =
            function
            | Some existing, Some incoming ->
                // update existing
               state
            | Some existing, None ->
                // remove existing
                state
            | None, Some incoming ->
                // create incoming
                state
            | None, None ->
                state


        MailboxProcessor.Start(fun inbox ->

            let rec loop (state:State) =
                async {
                    // retrieve the next message
                    let! message = inbox.Receive()

                    match message with

                    // the message is for this specific node
                    | Write { Write.path=[]; writer=writer; json=json; tick=tick } ->

                        let state =
                            match state.value.json, json with

                            // absorb unchanged data without incrementing tick 
                            // or notifiying listeners
                            | JsonValue.Boolean s, JsonValue.Boolean w when (s = w) -> state
                            | JsonValue.String s, JsonValue.String w when (s = w) -> state
                            | JsonValue.Number s, JsonValue.Number w when (s = w) -> state
                            | JsonValue.Record s, JsonValue.Record w when (s = w) -> state
                            | JsonValue.Array s, JsonValue.Array w  when (s = w) -> state
                            | JsonValue.Float s, JsonValue.Float w when (s = w) -> state
                            | JsonValue.Null, JsonValue.Null -> state

                            // merge child nodes
                            | JsonValue.Record s, JsonValue.Record w ->
                                let existing = s |> fromJsonRecord
                                let incoming = w |> fromJsonRecord
                                join existing incoming |> List.fold (mergeChild tick) state

                            // merge child indices
                            | JsonValue.Array s, JsonValue.Array w ->
                                let existing = s |> fromJsonArray
                                let incoming = w |> fromJsonArray

                                join existing incoming
                                |> List.fold (mergeChild tick) state

                            // create child nodes (replacing a primitive)
                            | _, JsonValue.Record w ->
                                let incoming = w |> fromJsonRecord

                                incoming
                                |> List.fold (createChild tick) (clear state)
                                |> write json tick

                            // create child indices (replacing a primitive)
                            | _, JsonValue.Array w ->
                                let incoming = w |> fromJsonArray

                                incoming
                                |> List.fold (createChild tick) (clear state)
                                |> write json tick

                            // nullify child nodes
                            | JsonValue.Record s, _ ->
                                state |> clear |> write json tick

                            // nullify child indices
                            | JsonValue.Array s, _ ->
                                state |> clear |> write json tick

                            // not replacing a record:
                            // update primitive data, and notify listeners
                            | _, JsonValue.Boolean _
                            | _, JsonValue.String _
                            | _, JsonValue.Number _
                            | _, JsonValue.Float _
                            | _, JsonValue.Null _ ->
                                state |> write json tick

                        // if the data was changed, notifier listeners
                        if state.value.tick >= tick then
                            notify state

                        // notify the writer of the outcome, if needed
                        match writer with
                        | Some writer -> writer.Reply(state.value)
                        | None -> ()

                        return! loop state

                    // the message is for a child node
                    | Write { Write.path=name::tail; writer=writer; json=json; tick=tick } ->

                        let state =
                            match state.value.json with
                            | JsonValue.Record s ->
                                state
                            | JsonValue.Array s ->

                                // hopefully name is an index in our array
                                // otherwise convert it to a key
                                match name |> Int64.TryParse with

                                // yes, it's an index:
                                | true, i ->
                                    // "resize array" if needed
                                    // never ever mutate it, always create a new one
                                    // or our MT guarantees break down
                                    state

                                // no, nullify our array, convert to
                                // a record, and dispatch tail
                                | false, _ ->
                                    state

                            | _ ->
                                // currently a primitive, convert to
                                // a record, and dispatch tail
                            
                                state

                        return! loop state

                    | Listen x ->
                        // update state with any new listeners
                        return! loop (state|> listen x)

                    | Stop ->
                        // return to stop
                        return ()
                }

            loop { value = Value.empty
                   children = Map.empty
                   listeners = Listeners.none })