namespace Nata.Core

open System.Text

type Codec<'In,'Out> = ('In->'Out)*('Out->'In)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Codec =

    let encoder (codec:Codec<'In,'Out>) =
        fst codec

    let decoder (codec:Codec<'In,'Out>) =
        snd codec

    let reverse ((encode,decode):Codec<'In,'Out>) : Codec<'Out,'In> =
        decode,encode

    let concatenate (bc:Codec<'B,'C>)
                    (ab:Codec<'A,'B>) : Codec<'A,'C> =
        let ac = (encoder ab) >> (encoder bc)
        let ca = (decoder bc) >> (decoder ab)
        ac,ca

    let Identity : Codec<'a,'a> =
        id, id
    
    let StringToBytes : Codec<string,byte[]> =
        (function null ->  ""  | x -> x) >> Encoding.UTF8.GetBytes,
        (function null -> [||] | x -> x) >> Encoding.UTF8.GetString
    let BytesToString : Codec<byte[],string> =
        reverse StringToBytes

    let Int32ToString : Codec<int32,string> =
        Int32.toString, Int32.ofString >> Option.getValueOr 0
    let StringToInt32 : Codec<string,int32> =
        reverse Int32ToString

    let Int64ToString : Codec<int64,string> =
        Int64.toString, Int64.ofString >> Option.getValueOr 0L
    let StringToInt64 : Codec<string,int64> =
        reverse Int64ToString