namespace Nata.Core

open System
open System.Text
open System.Security.Cryptography

module SHA256 =

    let h = 0x6a09e667u, 0xbb67ae85u, 0x3c6ef372u, 0xa54ff53au, 0x510e527fu, 0x9b05688cu, 0x1f83d9abu, 0x5be0cd19u

    let k =
        [ 0x428a2f98u; 0x71374491u; 0xb5c0fbcfu; 0xe9b5dba5u; 0x3956c25bu; 0x59f111f1u; 0x923f82a4u; 0xab1c5ed5u;
          0xd807aa98u; 0x12835b01u; 0x243185beu; 0x550c7dc3u; 0x72be5d74u; 0x80deb1feu; 0x9bdc06a7u; 0xc19bf174u;
          0xe49b69c1u; 0xefbe4786u; 0x0fc19dc6u; 0x240ca1ccu; 0x2de92c6fu; 0x4a7484aau; 0x5cb0a9dcu; 0x76f988dau;
          0x983e5152u; 0xa831c66du; 0xb00327c8u; 0xbf597fc7u; 0xc6e00bf3u; 0xd5a79147u; 0x06ca6351u; 0x14292967u;
          0x27b70a85u; 0x2e1b2138u; 0x4d2c6dfcu; 0x53380d13u; 0x650a7354u; 0x766a0abbu; 0x81c2c92eu; 0x92722c85u;
          0xa2bfe8a1u; 0xa81a664bu; 0xc24b8b70u; 0xc76c51a3u; 0xd192e819u; 0xd6990624u; 0xf40e3585u; 0x106aa070u;
          0x19a4c116u; 0x1e376c08u; 0x2748774cu; 0x34b0bcb5u; 0x391c0cb3u; 0x4ed8aa4au; 0x5b9cca4fu; 0x682e6ff3u;
          0x748f82eeu; 0x78a5636fu; 0x84c87814u; 0x8cc70208u; 0x90befffau; 0xa4506cebu; 0xbef9a3f7u; 0xc67178f2u ]

    let rotr(x:uint32, n) =
        assert(n < 32)
        (x >>> n) ||| (x <<< (32 - n))

    let ch(x:uint32, y:uint32, z:uint32) =
        (x &&& y) ^^^ ((~~~x) &&& z)

    let maj(x:uint32, y:uint32, z:uint32) =
        (x &&& y) ^^^ (x &&& z) ^^^ (y &&& z)

    let Σ0 x = rotr(x,2) ^^^ rotr(x,13) ^^^ rotr(x,22)
    let Σ1 x = rotr(x,6) ^^^ rotr(x,11) ^^^ rotr(x,25)

    let σ0 x = rotr(x,7) ^^^ rotr(x,18) ^^^ (x >>> 3)
    let σ1 x = rotr(x,17) ^^^ rotr(x,19) ^^^ (x >>> 10)

    let compress (a,b,c,d,e,f,g,h) (k,w) =
        let t1 = h + Σ1(e) + ch(e,f,g) + k + w
        let t2 = Σ0(a) + maj(a,b,c)
        (t1+t2,a,b,c,d+t1,e,f,g)

    let processChunk (h0,h1,h2,h3,h4,h5,h6,h7) (chunk:uint32[]) =
        assert(chunk.Length = 16)

        let w acc =
            let w0 =
                match acc with
                | _::w2::_::_::_::_::w7::_::_::_::_::_::_::_::w15::w16::_ ->
                    σ1(w2) + w7 + σ0(w15) + w16
                | init -> chunk.[init.Length]
            Some(w0, w0 :: acc)

        let (a,b,c,d,e,f,g,h) =
            Seq.unfold w []
            |> Seq.zip k
            |> Seq.fold compress (h0,h1,h2,h3,h4,h5,h6,h7)

        (h0+a, h1+b, h2+c, h3+d, h4+e, h5+f, h6+g, h7+h)

    let hashBytes (data:byte[]) =

        let data_bits = 8 * data.Length
        let pad_bits = 512 - ((data_bits + 1 + 64) % 512)

        let padding =
            Array.init ((1+pad_bits)/8) (function 0 -> 0x80uy | _ -> 0uy)
        let length =
            BitConverter.GetBytes(int64 data_bits)
            |> Array.rev

        let batch n =
            Seq.windowed n
            >> Seq.mapi (fun i x -> i,x)
            >> Seq.choose (fun (i,x) -> if i % n = 0 then Some x else None)

        let chunks =
            seq {
                yield! data
                yield! padding
                yield! length
            }
            |> batch 4
            |> Seq.map (fun x -> BitConverter.ToUInt32(Array.rev x,0))
            |> batch 16

        let (h0,h1,h2,h3,h4,h5,h6,h7) =
            chunks
            |> Seq.fold processChunk h

        [| h0; h1; h2; h3; h4; h5; h6; h7 |]
        |> Seq.collect (BitConverter.GetBytes >> Array.rev)
        |> Seq.toArray

    let referenceBytes (data:byte[]) =
        (new SHA256Managed()).ComputeHash(data)

    let private hex =
        Array.map (sprintf "%02x")
        >> String.Concat

    let private utf8 : string->byte[] =
        Encoding.UTF8.GetBytes

    let hash = hashBytes >> hex
    let hashUTF8Bytes = utf8 >> hashBytes
    let hashUTF8 = utf8 >> hash

    let reference = referenceBytes >> hex
    let referenceUTF8Bytes = utf8 >> referenceBytes
    let referenceUTF8 = utf8 >> reference
