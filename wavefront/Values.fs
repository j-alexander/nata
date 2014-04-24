namespace wavefront

type internal Data = Map<string * string, obj>

type Values(data : Data) =
    
    member x.Get<'a>(name : string) : Option<'a> =
        match Map.tryFind (typeof<'a>.FullName, name) data with
        | Some x -> Some (x :?> 'a)
        | None -> None

    member x.Set<'a>(name : string) (value : 'a) : Values =
        new Values(data |> Map.add (typeof<'a>.FullName, name) (value :> obj))

    member x.Reset<'a>(name:string) : Values =
        new Values(data |> Map.remove (typeof<'a>.FullName, name))

    member x.Keys() =
        new Keys(data |> Map.toSeq |> Seq.map fst |> Set.ofSeq)

    override x.ToString() =
        sprintf "values %s" ((Map.toList data).ToString())

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Values =

    let empty = new Values(Map.empty)

    let get<'a> name (values : Values) = values.Get<'a> name
    
    let set name value (values : Values) = values.Set name value

    let reset<'a> name (values : Values) = values.Reset<'a> name

    let keys (values : Values) = values.Keys()