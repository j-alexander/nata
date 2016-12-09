namespace Nata.Fun.JsonPath

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

            
    let private values (m:Match) (group:string) =
        [ for g in m.Groups.[group].Captures -> Int32.Parse g.Value ]

    let private valueOr (defaultValue:int) =
        function Some x -> x | None -> defaultValue


    type Query = string

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Query =
    
        type Levels = Level list
        and Level = Scope * Type
        and Type = Property of Name | Array of Index
        and Scope = Any | Exact
        and Name = string
        and Index =
            | Expression of string
            | Wildcard
            | Slice of start:int option * finish:int option * step:int
            | Literal of int list

        let levelsFor : Query -> Levels =

            let predicateFor : string -> Index =

                let (|Wildcard|_|) = function "*" -> Some Index.Wildcard | _ -> None
                let (|Literal|_|) =
                    let pattern = "^(?<i>-?\d+)(,(?<i>-?\d+))*$"
                    let regex = new Regex(pattern, RegexOptions.Compiled)
                    fun input ->
                        let index = regex.Match(input)
                        if index.Success then Some(Index.Literal(values index "i"))
                        else None
                let (|Slice|_|) =
                    let pattern = "^(?<start>-?\d+)?:(?<finish>-?\d+)?(:(?<step>-?\d+))?$"
                    let regex = new Regex(pattern, RegexOptions.Compiled)
                    fun input ->
                        let slice = regex.Match(input)
                        if slice.Success then
                            let step =
                                Seq.tryPick Some (values slice "step") |> valueOr 1
                            let start,finish =
                                Seq.tryPick Some (values slice "start"),
                                Seq.tryPick Some (values slice "finish")
                            match step with
                            | 0 -> Some(Index.Literal[])
                            | i -> Some(Index.Slice(start,finish,step))
                        else None
                let (|Expression|) input = Index.Expression input

                function
                | Wildcard x -> x
                | Literal x -> x
                | Slice x -> x
                | Expression x -> x

            let pattern = 
                "(?<quantifier>[\.]+)"+       // 1 or more '.' symbols
                "(?<name>([^.\[])*)"+         // anything other than a '.' or '['
                "(\[(?<predicate>[^\]]*)\])*" // and optionally:
                                              //   '['
                                              //   anything other than ']'
                                              //   ']'
            let regex = new Regex(pattern, RegexOptions.Compiled)

            fun (path:string) ->
                [
                    for x in regex.Matches(path) do
                        let name, quantifier =
                            x.Groups.["name"].Value,
                            x.Groups.["quantifier"].Value
                            |> function "." -> Exact | _ -> Any
                    
                        if (x.Groups.["predicate"].Success) then
                            let predicates = x.Groups.["predicate"].Captures
                            yield quantifier, Type.Property(name)
                            for x in predicates do
                                yield Exact, Type.Array (predicateFor x.Value)
                        else
                            yield quantifier, Type.Property(name)
                ]


    module Pattern  =

        type Automata = Automaton list
        and Automaton = Input->State list
        and State = Match | Automaton of Automaton
        and Input =
            | Property of Query.Name
            | Array of index:int*length:int

        let rec transition (levels:Query.Levels) =
            match levels with
            | [] -> fun _ -> []

            | (q,Query.Property(n)) :: tail ->
                function
                | Input.Array _ -> []
                | Input.Property name ->
                    match name with
                    | x when x=n || "*"=n ->
                        match tail with
                        | [] -> [ Match ]
                        | xs -> [ Automaton (transition xs) ]
                    | _ -> []
                    @
                    match q with
                    | Query.Any -> [ Automaton (transition levels) ]
                    | Query.Exact -> []
                
            | (q,Query.Array(p)) :: tail ->
                function
                | Input.Property _ -> []
                | Input.Array (index,length) ->
                    match p with
                    | Query.Index.Wildcard ->
                        match tail with
                        | [] -> [ Match ]
                        | xs -> [ Automaton (transition xs) ]
                    | Query.Index.Literal xs when
                        (xs
                         |> List.map (function x when x < 0 -> length+x | x -> x)
                         |> List.exists ((=) index)) ->
                        match tail with
                        | [] -> [ Match ]
                        | xs -> [ Automaton (transition xs) ]
                    | Query.Index.Slice(start,finish,step) when (step > 0) ->
                        let start =
                            match start |> valueOr 0 with
                            | x when x < 0 -> length+x
                            | x -> x
                        let finish =
                            match finish |> valueOr length with
                            | x when x <= 0 -> length+x
                            | x -> x
                        if (finish > index && index >= start) &&
                           (0 = (index-start) % step) then
                            match tail with
                            | [] -> [ Match ]
                            | xs -> [ Automaton (transition xs) ]
                        else []
                    | Query.Index.Expression _ -> []
                    @
                    match q with
                    | Query.Any -> [ Automaton (transition levels) ]
                    | Query.Exact -> []
                    
        let create (levels:Query.Levels) : State =
            Automaton (transition levels)


    let findSeq = 

        let rec recurse (states:Pattern.State list,value:JsonValue) =
            let isMatch, automata =
                states
                |> List.exists (function
                    | Pattern.State.Match -> true
                    | _ -> false),
                states
                |> List.choose (function
                    | Pattern.State.Automaton x -> Some(x)
                    | _ -> None)
            seq {
                if isMatch then
                    yield value
                yield!
                    match value with
                    | JsonValue.Record xs ->
                        xs
                        |> Seq.map(fun (name,json) ->
                            automata
                            |> List.collect(fun a -> 
                                a (Pattern.Input.Property name)),
                            json)
                        |> Seq.collect recurse
                    | JsonValue.Array xs ->
                        xs
                        |> Seq.mapi(fun i json ->
                            automata
                            |> List.collect(fun a ->
                                a (Pattern.Input.Array(i,xs.Length))),
                            json)
                        |> Seq.collect recurse
                    | _ -> Seq.empty         
            }
                
        Query.levelsFor >> function
        | [Query.Exact,Query.Property ""] -> Seq.singleton
        | (Query.Exact,Query.Property "")::levels
        | levels ->
            let start = Pattern.create levels
            fun json -> recurse([start],json)
            
    let findList query =
        findSeq query >> Seq.toList

    let find query =
        findSeq query >> Seq.head

    let tryFind query =
        findSeq query >> Seq.tryPick Some