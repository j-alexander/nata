namespace Nata.Core

open System
open System.Text
open System.Security.Cryptography

module BloomFilter =

    type Mutable private (bitMap:byte[]) =

        static let range = 19999999

        static let createHash =
            let range = bigint range
            let divisors =
                [ bigint 20000003
                  bigint 20000023
                  bigint 20000033
                  bigint 20000047 ]
            fun (input:string) ->
                let md5 = MD5.Create()
                let hash =
                    Encoding.UTF8.GetBytes(input)
                    |> md5.ComputeHash
                    |> bigint
                divisors
                |> List.map (fun d -> Math.Abs(int ((hash % d) % range)))

        let setBit (pos) =
            bitMap.[pos/8] <- byte (bitMap.[pos/8] ||| (1uy <<< (pos % 8)))

        let getBit (pos) =
            byte ((bitMap.[pos/8] &&& (1uy <<< (pos % 8))) >>> (pos % 8))

        new() =
            new Mutable(Array.zeroCreate range)

        new(f:Mutable) =
            new Mutable(Array.copy f.BitMap)

        member x.Clone() =
            new Mutable(x)

        member private x.BitMap
            with get() = bitMap

        member x.Add(key:string) =
            createHash(key)
            |> List.iter setBit

        member x.Contains(key:string) =
            createHash(key)
            |> List.map getBit
            |> List.forall ((=) 1uy)