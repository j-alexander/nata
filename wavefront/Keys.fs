namespace wavefront

open System
open System.Reflection

type internal KeySet = Set<string * string>

type Keys(set : KeySet) =

    member x.Add<'a>(name : string) : Keys =
        new Keys(set |> Set.add (typeof<'a>.FullName, name))

    member x.All() : seq<Type * string> =
        set |> Seq.map (fun (t, x) -> Type.GetType(t), x)

    member x.ForType<'a>() : seq<string> =
        set |> Seq.filter (fun (t, x) -> t = typeof<'a>.FullName) |> Seq.map snd

    member x.ForName(name : string) : seq<Type> =
        set |> Seq.filter (fun (t, x) -> x = name) |> Seq.map fst |> Seq.map Type.GetType

    member x.Remove<'a>(name : string) : Keys =
        new Keys(set |> Set.remove (typeof<'a>.FullName, name))

    override x.ToString() =
        sprintf "keys %s" ((Set.toList set).ToString())

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Keys =

    let empty = new Keys(Set.empty)
    
    let add<'a> name (keys : Keys) = keys.Add<'a> name

    let all (keys : Keys) = keys.All

    let forType<'a> (keys : Keys) = keys.ForType<'a>()

    let forName name (keys : Keys) = keys.ForName name

    let remove<'a> name (keys : Keys) = keys.Remove<'a> name