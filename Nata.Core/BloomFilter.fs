namespace Nata.Core

open System
open System.Text
open System.Security.Cryptography

type BloomFilter =
    private { Bits:Map<int,byte>; Range:int; Divisors:bigint list }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BloomFilter =

    type SizeMismatchException(l, r) =
        inherit Exception(sprintf "BloomFilter sizes do not match: %d <> %d" l r)

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

    /// a small size BloomFilter of ~ 2 million bits
    let small = emptyOfSize S
    /// a medium size BloomFilter of ~ 20 million bits
    let medium = emptyOfSize M
    /// a large size BloomFilter of ~ 200 million bits
    let large = emptyOfSize L

    /// a default BloomFilter of medium size (~ 20 million bits)
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

    /// expose the bloom filter to the specified value
    let add value f =
        hash value f
        |> List.fold setBit f

    /// expose the bloom filter to the specified sequence of values
    let addSeq (values:#seq<string>) f =
        values
        |> Seq.fold (fun f v -> add v f) f

    /// check wither the bloom filter has (probably) seen the specified value
    let contains value f =
        hash value f
        |> List.forall (getBit f)

    /// check whether the bloom filter has (probably) seen the specified sequence of values
    let containsAll (values:#seq<string>) f =
        values
        |> Seq.forall (fun v -> contains v f)

    /// check whether the bloom filter has (probably) seen any of the specified sequence of values
    let containsAny (values:#seq<string>) f =
        values
        |> Seq.exists (fun v -> contains v f)

    /// merge two bloom filters of the same size
    let union (l:BloomFilter) (r:BloomFilter) =
        if l.Range <> r.Range then
            raise (new SizeMismatchException(l.Range, r.Range))
        else
            let bits =
                seq {
                    for i in 0..(l.Range / 8) do
                        match Map.tryFind i l.Bits, Map.tryFind i r.Bits with
                        | None, None -> ()
                        | Some l, None -> yield i, l
                        | None, Some r -> yield i, r
                        | Some l, Some r -> yield i, (l ||| r)
                }
            { l with Bits=Map.ofSeq bits }