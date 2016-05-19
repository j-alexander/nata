namespace Nata.IO

open System
open System.Threading.Tasks

[<AutoOpen>]
module Core =

    let guid _ = Guid.NewGuid().ToString("n")

    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

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
                            |> Async.Catch
                            |> Async.StartAsTask
                            :> Task
                    let complete i =
                        tasks.[i] <- TaskCompletionSource().Task :> Task

                    [ 0 .. n-1 ]
                    |> List.iter receive

                    let receiving = ref n
                    while receiving.Value > 0 do 
                        let i = Task.WaitAny(tasks)
                        let result = (tasks.[i] :?> Task<Choice<'T option, exn>>).Result
                        match result with 
                        | Choice1Of2 (Some value) -> 
                            receive i
                            yield value
                        | Choice1Of2 (None) ->
                            complete i
                            receiving := receiving.Value - 1
                        | Choice2Of2 (exn) ->
                            raise exn
              }

    module Option =
        
        let whenTrue fn x = if fn x then Some x else None
        let coalesce snd fst = match fst with None -> snd | Some _ -> fst
        let filter fn = Option.bind(whenTrue fn)

        let getValueOr x = function None -> x | Some x -> x
        let getValueOrYield fn = function None -> fn() | Some x -> x

    module Null =
        
        let toOption = function null -> None | x -> Some x
        
    module Nullable =
        
        let map (fn : 'T -> 'U) (x : Nullable<'T>) : Nullable<'U> =
            if x.HasValue then Nullable(fn x.Value) else Nullable()

        let toOption (x:Nullable<'T>) : Option<'T> =
            if x.HasValue then Some x.Value else None
        let ofOption =
            function Some x -> Nullable(x) | _ -> Nullable()
    
    module Int64 =
        
        let ofString = Int64.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int64) = x.ToString()

    module Int32 =
        
        let ofString = Int32.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int32) = x.ToString()
        
    [<AutoOpen>]
    module Patterns =

        let (|Integer64|_|) = Int64.ofString
        let (|Integer32|_|) = Int32.ofString
        let (|Nullable|_|) = Nullable.toOption
