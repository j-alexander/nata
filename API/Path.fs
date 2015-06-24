namespace API

open System
open FSharp.Data


type Path = string list


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Path =

    let split = function | x when x |> String.IsNullOrEmpty -> []
                         | text ->
                             text.Split('/')
                             |> Seq.filter (String.IsNullOrEmpty >> not)
                             |> Seq.toList