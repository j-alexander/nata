namespace wavefront

type private Maps = Map<string, obj>

type Values(maps : Maps) =

    member private x.GetMap<'a>() =
        match Map.tryFind typeof<'a>.FullName maps with
        | Some(map) -> map :?> Map<string, 'a>
        | None -> Map.empty

    member private x.SetMap<'a>(map : Map<string, 'a>) =
        Map.add typeof<'a>.FullName (map :> obj) maps
    
    member x.Get<'a>(name : string) : Option<'a> =
        x.GetMap<'a>() |> Map.tryFind name

    member x.Set<'a>(name : string) (value : 'a) : Values =
        new Values(x.GetMap<'a>() |> Map.add name value |> x.SetMap)

    override x.ToString() =
        maps.ToString()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Values =

    let empty = new Values(Map.empty)
    
    let set name value (values : Values) = values.Set name value

    let get name (values : Values) = values.Get name