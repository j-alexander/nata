namespace Nata.Core

open System

[<AutoOpen>]
module Patterns =

    let (|Nullable|_|) = Nullable.toOption
    let (|Boolean|_|) = Boolean.ofString
    let (|Decimal|_|) = Decimal.ofString
    let (|Integer64|_|) = Int64.ofString
    let (|Integer32|_|) = Int32.ofString
    let (|DateTime|_|) = DateTime.ofString
    let (|JsonValue|_|) = JsonValue.tryParse

    let (|AggregateException|_|) : exn -> exn option =
        function
        | :? AggregateException as e -> Some e.InnerException
        | _ -> None

    let (|AbsoluteUri|_|), (|RelativeUri|_|), (|Uri|_|) =

        let ofKind (kind:UriKind) (x:string) =
            match Uri.TryCreate(x, kind) with
            | true, value -> Some value | false, _ -> None

        ofKind UriKind.Absolute,
        ofKind UriKind.Relative,
        ofKind UriKind.RelativeOrAbsolute
