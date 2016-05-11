namespace Nata.IO

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
        Encoding.UTF8.GetBytes, Encoding.UTF8.GetString

    let BytesToString : Codec<byte[],string> =
        Encoding.UTF8.GetString, Encoding.UTF8.GetBytes
