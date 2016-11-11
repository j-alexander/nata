namespace Nata.Core

open System
open System.Threading.Tasks

[<AutoOpen>]
module Core =

    let guid _ = Guid.NewGuid().ToString("n")

    let swap fn i j = fn j i
    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

    let filterFst fn (i,j) = fn i
    let filterSnd fn (i,j) = fn j

    let chooseFst fn (i,j) = fn i |> Option.map (fun i -> i,j)
    let chooseSnd fn (i,j) = fn j |> Option.map (fun j -> i,j)

    module Seq =

        let mapFst fn = Seq.map <| mapFst fn
        let mapSnd fn = Seq.map <| mapSnd fn

        let filterFst fn = Seq.filter <| filterFst fn
        let filterSnd fn = Seq.filter <| filterSnd fn

        let chooseFst fn = Seq.choose <| chooseFst fn
        let chooseSnd fn = Seq.choose <| chooseSnd fn
          
        let log fn = Seq.map (fun x -> fn x; x)
        let logi fn = Seq.mapi (fun i x -> fn i x; x)

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

        let between (left_inclusive:int64, right_inclusive:int64) (x:int64) =
            let lower, upper =
                Math.Min(left_inclusive, right_inclusive),
                Math.Max(left_inclusive, right_inclusive)
            Math.Max(lower, Math.Min(upper, x))
        
        let ofString = Int64.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int64) = x.ToString()

    module Int32 =

        let between (left_inclusive:int32, right_inclusive:int32) (x:int32) =
            let lower, upper =
                Math.Min(left_inclusive, right_inclusive),
                Math.Max(left_inclusive, right_inclusive)
            Math.Max(lower, Math.Min(upper, x))
        
        let ofString = Int32.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int32) = x.ToString()

    module String =
        
        let contains (substring:string) (x:string) =
            match x, substring with
            | null, null -> true
            | null, _ | _, null -> false
            | x, substring -> x.Contains(substring)
            
        let containsIgnoreCase (substring:string) (x:string) =
            match x, substring with
            | null, null -> true
            | null, _ | _, null -> false
            | x, substring -> x.ToLowerInvariant().Contains(substring.ToLowerInvariant())

        let split (delimiter:char) : string->string list =
            function null -> [] | x -> x.Split(delimiter) |> Array.toList
        
    [<AutoOpen>]
    module Patterns =

        let (|Integer64|_|) = Int64.ofString
        let (|Integer32|_|) = Int32.ofString
        let (|Nullable|_|) = Nullable.toOption
