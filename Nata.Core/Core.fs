namespace Nata.Core

open System
open System.Collections.Generic
open System.Runtime
open System.Threading.Tasks

[<AutoOpen>]
module Core =

    let guid _ = Guid.NewGuid().ToString("n")
    let guidBytes _ = Guid.NewGuid().ToByteArray()

    let swap fn i j = fn j i
    let mapFst fn (i,j) = fn i, j
    let mapSnd fn (i,j) = i, fn j

    let filterFst fn (i,j) = fn i
    let filterSnd fn (i,j) = fn j

    let chooseFst fn (i,j) = fn i |> Option.map (fun i -> i,j)
    let chooseSnd fn (i,j) = fn j |> Option.map (fun j -> i,j)

    module Seq =

        let tryHead xs = Seq.tryPick Some xs

        let mapFst fn = Seq.map <| mapFst fn
        let mapSnd fn = Seq.map <| mapSnd fn

        let filterFst fn = Seq.filter <| filterFst fn
        let filterSnd fn = Seq.filter <| filterSnd fn

        let chooseFst fn = Seq.choose <| chooseFst fn
        let chooseSnd fn = Seq.choose <| chooseSnd fn
          
        let log fn = Seq.map (fun x -> fn x; x)
        let logi fn = Seq.mapi (fun i x -> fn i x; x)

        let trySkip n =
            Seq.mapi (fun i x -> i,x)
            >> Seq.filter (fun (i,x) -> i >= n)
            >> Seq.map snd

        let consume (sequences:#seq<'T> list) : seq<'T> =
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
                        tasks.[i] <- TaskCompletionSource().Task

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

        let mergeBy (fn:'a->'b) (l : #seq<'a>) (r : #seq<'a>) =
            seq {
                use l = l.GetEnumerator()
                use r = r.GetEnumerator()

                let lNext = ref <| l.MoveNext()
                let rNext = ref <| r.MoveNext()

                let next (enumerator : IEnumerator<'a>) flag =
                    let value = enumerator.Current
                    flag := enumerator.MoveNext()
                    value
                let nextL() = next l lNext
                let nextR() = next r rNext

                while !lNext || !rNext do
                    match !lNext, !rNext with
                    | true, true ->
                        if fn(l.Current) > fn(r.Current) then yield nextR()
                        elif fn(l.Current) < fn(r.Current) then yield nextL()
                        else yield nextL(); yield nextR()
                    | true, false -> yield nextL()
                    | false, true -> yield nextR()
                    | false, false -> ()
            }

        let merge l r = mergeBy id l r

        let changesBy (fn:'a->'b) (xs:#seq<'a>) =
            seq {
                use enumerator = xs.GetEnumerator()
                let rec loop last =
                    seq {
                        match enumerator.MoveNext() with
                        | false -> ()
                        | true ->
                            let next = fn enumerator.Current
                            match last with
                            | Some last when last <> next ->
                                yield enumerator.Current
                            | None -> 
                                yield enumerator.Current
                            | _ -> ()
                            yield! loop (Some next)
                    }
                yield! loop None
            }

        let changes xs = changesBy id xs

        // pairwise with the previous value, if it exists.
        // e.g. [0..2] -> [(None, 0); (Some 0; 1); (Some 1; 2)]
        let delta (xs:#seq<'a>) =
            seq {
                yield None
                yield! Seq.map Some xs
            }
            |> Seq.pairwise
            |> Seq.choose (function (_, None) -> None | (last, Some next) -> Some (last, next))
            

    module Option =
        
        let whenTrue fn x = if fn x then Some x else None

        let coalesce snd fst = match fst with None -> snd | x -> x
        let coalesceWith snd fst = match fst with None -> snd() | x -> x

        let defaultValue x = function None -> x | Some x -> x
        let defaultWith fn = function None -> fn() | Some x -> x

        let tryFunction fn x = try Some <| fn x with _ -> None
        
        let distribute = function Some(x,y) -> Some x, Some y | None -> None, None
        let join = function Some (Some x) -> Some x | _ -> None

    module Null =
        
        let toOption = function null -> None | x -> Some x
        
    module Nullable =
        
        let map (fn : 'T -> 'U) (x : Nullable<'T>) : Nullable<'U> =
            if x.HasValue then Nullable(fn x.Value) else Nullable()

        let toOption (x:Nullable<'T>) : Option<'T> =
            if x.HasValue then Some x.Value else None
        let ofOption =
            function Some x -> Nullable(x) | _ -> Nullable()

    module Boolean =

        let ofString : string -> bool option = Boolean.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:bool) = x.ToString()

    module Decimal =

        let between (left_inclusive:decimal, right_inclusive:decimal) (x:decimal) =
            let lower, upper =
                Math.Min(left_inclusive, right_inclusive),
                Math.Max(left_inclusive, right_inclusive)
            Math.Max(lower, Math.Min(upper, x))
        
        let ofString : string -> decimal option = Decimal.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:decimal) = x.ToString()
    
    module Int64 =

        let between (left_inclusive:int64, right_inclusive:int64) (x:int64) =
            let lower, upper =
                Math.Min(left_inclusive, right_inclusive),
                Math.Max(left_inclusive, right_inclusive)
            Math.Max(lower, Math.Min(upper, x))
        
        let ofString : string -> int64 option = Int64.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int64) = x.ToString()

    module Int32 =

        let between (left_inclusive:int32, right_inclusive:int32) (x:int32) =
            let lower, upper =
                Math.Min(left_inclusive, right_inclusive),
                Math.Max(left_inclusive, right_inclusive)
            Math.Max(lower, Math.Min(upper, x))
        
        let ofString : string -> int32 option = Int32.TryParse >> function true, x -> Some x | _ -> None
        let toString (x:int32) = x.ToString()

    module String =

        let replace (before:string) (after:string) (x:string) =
          match x with
          | null -> null
          | x -> x.Replace(before, after)

        let remove (remove:string) = replace remove ""

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

        let trySubstring index length (x:string) =
            match x with
            | null -> None
            | x when (x.Length < index ||
                      x.Length < index+length) -> None
            | x when (0 > index ||
                      0 > length) -> None
            | x -> Some(x.Substring(index, length))

        let tryStartAt index (x:string) =
            match x with
            | null -> None
            | x when (index > x.Length ||
                      index < 0) -> None
            | x -> Some(x.Substring(index))

    module Async =

        let reraise e =
            ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw()
            failwithf "Async.reraise %A failed." e

    module Task =

        let waitAsync (task:Task) : Async<unit> =
            Async.FromContinuations(fun (success,error,_) ->
                task.ContinueWith(fun (task:Task) ->
                    if task.IsFaulted then
                        let e = task.Exception
                        if e.InnerExceptions.Count = 1 then error e.InnerExceptions.[0]
                        else error e
                    elif task.IsCanceled then
                        error(new TaskCanceledException())
                    else
                        success())
                |> ignore)

        let waitForResultAsync (task:Task<'T>) : Async<'T> =
            Async.FromContinuations(fun (success,error,_) ->
                task.ContinueWith(fun (task:Task<'T>) ->
                    if task.IsFaulted then
                        let e = task.Exception
                        if e.InnerExceptions.Count = 1 then error e.InnerExceptions.[0]
                        else error e
                    elif task.IsCanceled then
                        error(new TaskCanceledException())
                    else
                        success task.Result)
                |> ignore)

        let wait t =
            t |> waitAsync |> Async.RunSynchronously

        let waitForResult t =
            t |> waitForResultAsync |> Async.RunSynchronously