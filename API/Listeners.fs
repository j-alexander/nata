namespace API


type private Listeners = Map<string option, Listen list>


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Listeners =

    let private lookup (name:string option) : Listeners -> Listen list =
        Map.tryFind name >> function | None -> []
                                     | Some x -> x  

    let private append (name:string option)
                       (listener:Listen)
                       (listeners:Listeners) : Listeners =
        Map.add name (List.append listener (lookup name listeners)) listeners


    // find the listeners for the current node
    let get = (lookup None) >> List.map (fun x -> x.listener)

    // find the listeners for a particular child
    let getChild name = lookup (Some name)

    // represents no listeners at all
    let none : Listeners = Map.empty

    // add a listener
    let add = append None

    // add a listener for a particular child
    let addChild name = append (Some name)