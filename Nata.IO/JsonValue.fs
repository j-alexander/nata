namespace Nata.IO

open System.Text
open FSharp.Data

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    let toString (json:JsonValue) = json.ToString(JsonSaveOptions.DisableFormatting)
    let toBytes =  toString >> Encoding.Default.GetBytes
