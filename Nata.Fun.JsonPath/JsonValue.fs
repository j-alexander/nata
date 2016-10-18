namespace Nata.Fun.JsonPath

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =


    type Query = string

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Query =
    
        type Levels = Level list
        and Level = Quantifier * Type
        and Type = Property of Name | Array of Predicate
        and Quantifier = All | Exists
        and Name = string
        and Predicate = string

        let levelsFor : Query -> Levels =
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
                            |> function "." -> Exists | _ -> All
                    
                        if (x.Groups.["predicate"].Success) then
                            let predicates = x.Groups.["predicate"].Captures
                            yield quantifier, Type.Property(name)
                            for predicate in predicates do
                                yield Exists, Type.Array (predicate.Value)
                        else
                            yield quantifier, Type.Property(name)
                ]


    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
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
                    | Query.All -> [ Automaton (transition levels) ]
                    | Query.Exists -> []
                
            | (q,Query.Array(p)) :: tail ->
                function
                | Input.Property _ -> []
                | Input.Array (index,length) ->
                    match (index,length) with
                    | _ when "*"=p ->
                        match tail with
                        | [] -> [ Match ]
                        | xs -> [ Automaton (transition xs) ]
                    | _ -> []
                    @
                    match q with
                    | Query.All -> [ Automaton (transition levels) ]
                    | Query.Exists -> []
                    
        let create (levels:Query.Levels) : State =
            Automaton (transition levels)


    let find = 

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
        | [Query.Exists,Query.Property ""] -> fun json -> [json]
        | (Query.Exists,Query.Property "")::levels
        | levels ->
            let start = Pattern.create levels
            fun json ->
                recurse([start],json)
                |> Seq.toList