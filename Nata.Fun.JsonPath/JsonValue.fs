namespace Nata.Fun.JsonPath

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    type Query = string
    and Index = int
    and Length = int

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

    type FSA = Match | Automaton of Automaton
    and Automata = Automaton list
    and Automaton = FSAType->List<FSA>
    and FSAType =
        | FSANode of Name
        | FSAArray of Index * Length

    let rec create (levels:Levels) =
        match levels with
        | [] -> fun _ -> []

        | (q,Property(n)) :: tail ->
            function
            | FSAArray _ -> []
            | FSANode name ->
                match name with
                | x when x=n || "*"=n ->
                    match tail with
                    | [] -> [ Match ]
                    | xs -> [ Automaton (create xs) ]
                | _ -> []
                @
                match q with
                | All -> [ Automaton (create levels) ]
                | Exists -> []
                
        | (q,Array(p)) :: tail ->
            function
            | FSANode _ -> []
            | FSAArray (index,length) ->
                match (index,length) with
                | _ when "*"=p ->
                    match tail with
                    | [] -> [ Match ]
                    | xs -> [ Automaton (create xs) ]
                | _ -> []
                @
                match q with
                | All -> [ Automaton (create levels) ]
                | Exists -> []



    let find = 

        let rec recurse (fsas,value) =
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
                    yield value
                match value with
                | JsonValue.Record xs ->
                    yield!
                        xs
                        |> Seq.map(fun (name,json) ->
                            automata
                            |> List.collect(fun a -> a (FSANode name)),
                            json)
                        |> Seq.collect recurse
                | JsonValue.Array xs ->
                    yield!
                        xs
                        |> Seq.mapi(fun i json ->
                            automata
                            |> List.collect(fun a -> a (FSAArray(i,xs.Length))),
                            json)
                        |> Seq.collect recurse
                | _ -> ()           
            }
                
        levelsFor >> function
        | [Exists,Property ""] -> fun json -> [json]
        | (Exists,Property "")::levels
        | levels ->
            create levels
            |> fun fsas json ->
                recurse([Automaton(fsas)],json)
                |> Seq.toList