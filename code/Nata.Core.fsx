(**
# Overview
*)
(*** hide ***)
open System
(**
# Package
1. *Nata.Core* from [NuGet](https://www.nuget.org/packages/Nata.Core/), or using [Paket](https://fsprojects.github.io/Paket/):

 ```powershell
   ./.paket/paket.exe add nuget Nata.Core project MyProject
 ```

2. *Reference* and open the Nata.Core library:
*)
#r "packages/Nata.Core/lib/net461/Nata.Core.dll"
open Nata.Core
(**
*)
(*** hide ***)
#r "packages/Nata.IO/lib/net461/Nata.IO.dll"
open Nata.IO
type SomeType = unit
type SomeOtherType = unit
type KeyT = SomeType
type DataT = SomeType
type IndexT = SomeType
type NewDataT = SomeOtherType
type NewIndexT = SomeOtherType
(**
# Extensions

- generate a unique id, or the bytes of one
*)
guid()
// val it : string = "b2bb73b79ad04871a75a20a478fb3c8c"

guidBytes()
// val it : byte [] = [|131uy; 238uy; 246uy; 245uy; 252uy; ...

// e.g., shuffle a list
[1..10] |> List.sortBy guid
(*** hide ***)
let keySeq : seq<KeyT> = Seq.empty
let map : Map<KeyT, DataT> = Map.empty
(**
- `swap` the parameters of a function, to make pipelining easier
*)
// e.g. look up a long sequence of keys
let dataSeq = keySeq |> Seq.choose (map |> swap Map.tryFind)
(**
- compose functions to `mapFst` or `mapSnd` a tuple
*)
let unpack : (Event<DataT>*IndexT) -> (DataT*IndexT) =
    mapFst Event.data
(**
## Seq
*)
(*** hide ***)
let events: seq<Event<DataT> * IndexT> = Seq.empty
(**
- use `Seq.mapFst` or `Seq.mapSnd` to transform just the data or index of a seq
*)
let data = events |> Seq.mapFst Event.data
(*** hide ***)
let isGood : DataT -> bool = fun _ -> false
(**
- similarly use `Seq.filterFst/Snd` or `Seq.chooseFst/Snd` to select elements by data or index:
*)
events |> Seq.filterFst (Event.data >> isGood)
(*** hide ***)
let blockedSeq = [1]
let readySeq = [2..3]
let calculate : int->unit = ignore
(**
- `Seq.consume` from multiple sequences as data becomes available
*)
[ blockedSeq; blockedSeq; readySeq; blockedSeq ]
|> Seq.consume
|> Seq.iter calculate // immediately calculates a value from readySeq
(**
- `Seq.merge` pulls from left or right based on whichever value comes first
*)
Seq.merge [ 1; 2; 3; 7; 1; ] [ 0; 5; 8; 8 ]
// val it : seq<int> = seq [0; 1; 2; 3; 5; 7; 1; 8; 8]
(*** hide ***)
let leftEvents : seq<Event<DataT> * IndexT> = Seq.empty
let rightEvents : seq<Event<DataT> * IndexT> = Seq.empty
(**
- `Seq.mergeBy` can be used to correctly merge streams of timeseries data
*)
let dataByTime =
    Seq.mergeBy snd leftEvents rightEvents
    |> Seq.map (fst >> Event.data)
(**
## Option

## Null

## Nullable

## Decimal

## Int64

## Int32

## String

# Codec

# JsonValue

# DateTime

## DateTime.Resolution

## DateTime.Codec

# Patterns

# GZip

## GZip.Stream

## GZip.File
*)
