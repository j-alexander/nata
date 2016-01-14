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

    let Identity : Codec<'a,'a> =
        id, id
    
    let StringToBytes : Codec<string,byte[]> =
        Encoding.Default.GetBytes, Encoding.Default.GetString

    let BytesToString : Codec<byte[],string> =
        Encoding.Default.GetString, Encoding.Default.GetBytes
