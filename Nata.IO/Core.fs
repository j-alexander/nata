namespace Nata.IO

open System
open System.Threading.Tasks

[<AutoOpen>]
module Core =

    let guid _ = Guid.NewGuid().ToString("n")

    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Seq =

        let mapFst fn = Seq.map <| mapFst fn
        let mapSnd fn = Seq.map <| mapSnd fn
          
        let merge (sequences:seq<'T> list) : seq<'T> =
            seq { 
                let n = sequences.Length
                if n > 0 then 
                    let enumerators = [| for sequence in sequences -> sequence.GetEnumerator()  |]
                    use disposables =
                        { 
                            new IDisposable with
                                member x.Dispose() =
                                    match
                                        enumerators
                                        |> Array.map (fun enumerator ->
                                            try enumerator.Dispose()
                                                None
                                            with e ->
                                                Some e)
                                        |> Array.tryPick id with
                                    | Some x -> raise x
                                    | None -> ()
                        }
                    let tasks = Array.zeroCreate n

                    let receive i =
                        tasks.[i] <-
                            async {
                                if enumerators.[i].MoveNext() then
                                    return Some enumerators.[i].Current
                                else
                                    return None
                            }
                            |> Async.StartAsTask
                            :> Task
                    let complete i =
                        tasks.[i] <- TaskCompletionSource().Task :> Task

                    [ 0 .. n-1 ]
                    |> List.iter receive

                    let receiving = ref n
                    while receiving.Value > 0 do 
                        let i = Task.WaitAny(tasks)
                        let result = (tasks.[i] :?> Task<'T option>).Result
                        match result with 
                        | Some value -> 
                            receive i
                            yield value
                        | None ->
                            complete i
                            receiving := receiving.Value - 1
              }

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Option =
        
        let whenTrue fn x = if fn x then Some x else None
        let coalesce snd fst = match fst with None -> snd | Some _ -> fst
        let filter fn = Option.bind(whenTrue fn)

        let bindNone fn = function None -> fn() | Some x -> x
        let getValueOr x = function None -> x | Some x -> x
        
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Nullable =
        
        let map (fn : 'T -> 'U) (x : Nullable<'T>) : Nullable<'U> =
            if x.HasValue then Nullable(fn x.Value) else Nullable()
            