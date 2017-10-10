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
type DataT = unit
type IndexT = unit
(**
# Extensions

## Seq
*)
(*** hide ***)
type NewDataT = unit
type NewIndexT = unit
let events : seq<Event<DataT> * IndexT> = Seq.empty
(**
*)
let data = events |> Seq.mapFst Event.data

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
