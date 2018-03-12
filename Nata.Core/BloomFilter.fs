namespace Nata.Core

open System
open System.Text
open System.Security.Cryptography

type BloomFilter =
    private { Bits:Map<int,byte>; Range:int; Divisors:bigint list }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BloomFilter =

    type Size =
        /// ~ 2 million bits
        | S
        /// ~ 20 million bits
        | M
        /// ~ 200 million bits
        | L

    let emptyOfSize =
        function // use prime numbers here:
        | S -> 1999993, [ 2000003; 2000029; 2000039; 2000081 ]
        | M -> 19999999, [ 20000003; 20000023; 20000033; 20000047 ]
        | L -> 199999991, [ 200000033; 200000039; 200000051; 200000069 ]
        >>
        fun (range, divisors) ->
          { Bits=Map.empty
            Range=range
            Divisors=divisors |> List.map bigint }

    let small = emptyOfSize S
    let medium = emptyOfSize M
    let large = emptyOfSize L

    /// a medium size BloomFilter of ~ 20 million bits
    let empty = medium

    let private check range index =
        if index < 0 then
            raise (new IndexOutOfRangeException(sprintf "%d must not be negative" index))
        elif index > range then
            raise (new IndexOutOfRangeException(sprintf "%d must not exceed %d" index range))
        else index

    let private getBit { Bits=bits; Range=range } =
        check range >> fun index ->
            let value = Map.tryFind (index/8) bits |> Option.defaultValue 0uy
            byte ((value &&& (1uy <<< (index % 8))) >>> (index % 8))
            |> (=) 1uy

    let private setBit ({ Bits=bits; Range=range } as f) =
        check range >> fun index ->
            let value = Map.tryFind (index/8) bits |> Option.defaultValue 0uy
            let bits = Map.add (index/8) (byte (value ||| (1uy <<< (index % 8)))) bits
            { f with Bits=bits }

    let private hash (value:string) { Range=range; Divisors=divisors } =
        use md5 = MD5.Create()
        let hash =
            Encoding.UTF8.GetBytes(value)
            |> md5.ComputeHash
            |> bigint
        divisors
        |> List.map (fun d -> Math.Abs((int (hash % d)) % range))

    let add value f =
        hash value f
        |> List.fold setBit f

    let contains value f =
        hash value f
        |> List.forall (getBit f)