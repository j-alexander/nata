namespace Nata.Fun.JsonPath

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    type Query = string

    type Levels = Level list
    and Level = Quantifier * Type
    and Type = Node of Name | Array of Name * Predicate
    and Quantifier = All | Exists
    and Name = string
    and Predicate = string

    let levelsFor : Query -> Levels =
        let pattern = 
            "(?<quantifier>[\.]+)"+     // 1 or more '.' symbols
            "(?<name>([^.\[])*)"+       // anything other than a '.' or '['
            "(?<predicate>\[[^\]]*\])?" // and optionally:
                                        //   '['
                                        //   anything other than ']'
                                        //   ']'
        let regex = new Regex(pattern, RegexOptions.Compiled)
        fun (path:string) ->
            [
                for x in regex.Matches(path) ->
                    let name, quantifier =
                        x.Groups.["name"].Value,
                        x.Groups.["quantifier"].Value
                        |> function "." -> Exists | _ -> All
                    
                    if (x.Groups.["predicate"].Success) then
                        let predicate = x.Groups.["predicate"].Value
                        quantifier, Type.Array (name,predicate)
                    else
                        quantifier, Type.Node (name)
            ]

    type FSA = Match | Automaton of (Name->List<FSA>)

    let rec create (levels:Levels) =
        match levels with
        | [] -> fun _ -> []

        | (q,Node(n)) :: tail ->
            fun name ->
                match name with
                | x when x=n ->
                    match tail with
                    | [] -> [ Match ]
                    | xs -> [ Automaton (create xs) ]
                | _ -> []
                @
                match q with
                | All -> [ Automaton (create levels) ]
                | Exists -> []
                
        // TODO: apply predicate for array
        | (q,Array(n,p)) :: tail ->
            fun name ->
                match name with
                | x when x=n ->
                    match tail with
                    | [] -> [ Match ]
                    | xs -> [ Automaton (create xs) ]
                | _ -> []
                @
                match q with
                | All -> [ Automaton (create levels) ]
                | Exists -> []



    let find = 

        let rec recurse (fsas,json) =
            let isMatch, automata =
                fsas
                |> List.exists (function
                    | Match -> true
                    | _ -> false),
                fsas
                |> List.choose (function
                    | Automaton x -> Some(x)
                    | _ -> None)
            seq {
                if isMatch then
                    yield json
                match json with
                | JsonValue.Record xs ->
                    yield!
                        xs
                        |> Seq.map(fun (name,json) ->
                            automata
                            |> List.collect(fun a -> a name),
                            json)
                        |> Seq.collect recurse
                | _ -> ()           
            }
                

        levelsFor >> create >> fun fsas json ->
            recurse([Automaton(fsas)],json)
            |> Seq.toList