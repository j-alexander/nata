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
    and Type = Node of Name | Array of Name * Predicate
    and Quantifier = All | Exists
    and Name = string
    and Predicate = string

    let levelsFor : Query -> Levels =
        let pattern = 
            "(?<quantifier>[\.]+)"+       // 1 or more '.' symbols
            "(?<name>([^.\[])*)"+         // anything other than a '.' or '['
            "(\[(?<predicate>[^\]]*)\])?" // and optionally:
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

    type FSA = Match | Automaton of Automaton
    and Automata = Automaton list
    and Automaton = FSAType->List<FSA>
    and FSAType =
        | FSANode of Name
        | FSAArray of Name * Index * Length

    let rec create (levels:Levels) =
        match levels with
        | [] -> fun _ -> []

        | (q,Node(n)) :: tail ->
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
                
        | (q,Array(n,p)) :: tail ->
            function
            | FSANode _ -> []
            | FSAArray (name,index,length) ->
                match name with
                | x when (x=n || "*"=n) && "*"=p ->
                    match tail with
                    | [] -> [ Match ]
                    | xs -> [ Automaton (create xs) ]
                | _ -> []
                @
                match q with
                | All -> [ Automaton (create levels) ]
                | Exists -> []



    let find = 

        let rec recurse (key,fsas,value) =
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
                            name,
                            automata
                            |> List.collect(fun a -> a (FSANode name)),
                            json)
                        |> Seq.collect recurse
                | JsonValue.Array xs ->
                    yield!
                        xs
                        |> Seq.mapi(fun i json ->
                            key,
                            automata
                            |> List.collect(fun a -> a (FSAArray(key,i,xs.Length))),
                            json)
                        |> Seq.collect recurse
                | _ -> ()           
            }
                

        function
        | "$." -> fun json -> [json]
        | query ->
            query 
            |> levelsFor 
            |> create
            |> fun fsas json ->
                recurse(String.Empty,[Automaton(fsas)],json)
                |> Seq.toList