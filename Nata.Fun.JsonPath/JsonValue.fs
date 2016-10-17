namespace Nata.Fun.JsonPath

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    type Level = Quantifier * Type
    and Type = Node of Name | Array of Name * Predicate
    and Quantifier = All | Exists
    and Name = string
    and Predicate = string

    let levelsFor : string -> Level list =
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
    
    let find (path:string) (json:JsonValue) =

        []