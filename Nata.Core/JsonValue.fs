namespace Nata.Core

open System
open System.Text
open FSharp.Data
open Newtonsoft.Json

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JsonValue =

    module Converters =
    
        open FSharp.Reflection

        type public OptionConverter() =
            inherit JsonConverter()

            override x.CanConvert(t) = 
                t.IsGenericType &&
                t.GetGenericTypeDefinition() = typedefof<option<_>>

            override x.WriteJson(writer, value, serializer) =
                let value = 
                    if isNull value then null
                    else 
                        let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                        fields.[0]  
                serializer.Serialize(writer, value)

            override x.ReadJson(reader, t, existingValue, serializer) =        
                let innerType = t.GetGenericArguments().[0]   
                match innerType.IsValueType, reader.Value with
                | true, null ->
                    None :> obj
                | _ -> 
                    try let value = serializer.Deserialize(reader, innerType)
                        let cases = FSharpType.GetUnionCases(t)
                        if isNull value then FSharpValue.MakeUnion(cases.[0], [||])
                        else FSharpValue.MakeUnion(cases.[1], [|value|])
                    with _ -> None :> obj


        type public TupleConverter() =
            inherit JsonConverter()
    
            override x.CanConvert(t:Type) = 
                FSharpType.IsTuple(t)

            override x.WriteJson(writer, value, serializer) =
                serializer.Serialize(writer, FSharpValue.GetTupleFields(value))

            override x.ReadJson(reader, t, _, serializer) =
                let elements =
                    [|
                        reader.Read() |> ignore
                        for element in FSharpType.GetTupleElements(t) do
                            yield serializer.Deserialize(reader, element)
                            reader.Read() |> ignore
                    |]
               
                FSharpValue.MakeTuple(elements, t)
        

        type public ValueAttribute(value : obj, [<ParamArray>]additional : obj array) =
            inherit Attribute()

            new(value : obj) = ValueAttribute(value, [||])

            member x.Values = [ yield value
                                yield! additional ]


        type public UnionConverter() =
            inherit JsonConverter()

            override x.CanConvert(t : Type) =
                FSharpType.IsUnion(t) &&
                FSharpType.GetUnionCases(t) |> Seq.forall (fun z -> z.GetFields().Length = 0)

            override x.WriteJson(writer : JsonWriter, value, serializer) =
                let case, fields = FSharpValue.GetUnionFields(value, value.GetType())
                let values =
                    case.GetCustomAttributes()
                    |> Seq.choose(function :? ValueAttribute as x -> Some x | _ -> None)
                    |> Seq.toList
                match values with
                | x :: _ -> match x.Values.Head with
                            | :? int as value -> writer.WriteValue(value)
                            | :? string as value -> writer.WriteValue(value)
                            | _ -> writer.WriteValue(case.Name)
                | [] -> writer.WriteValue(case.Name)

            override x.ReadJson(reader : JsonReader, objectType, existingValue, serializer) =
                let value = reader.Value.ToString()
                FSharpType.GetUnionCases(objectType)
                |> List.ofSeq
                |> List.filter(fun case ->
                    let values =
                        case.GetCustomAttributes()
                        |> Seq.choose(function :? ValueAttribute as x -> Some x | _ -> None)
                        |> Seq.toList
                    match values with
                    | x :: _ ->

                        x.Values
                        |> Seq.exists(function | :? string as x -> 0 = String.Compare(x, value, true)
                                               | :? int as x -> match value |> Int32.TryParse with | (true, y) -> (x = y)
                                                                                                   | _ -> false
                                               | _ -> false)

                    | _ -> 0 = String.Compare(case.Name, value, true))
                |> List.map(fun x -> FSharpValue.MakeUnion(x, [||]))
                |> function | [] -> value |> sprintf "No union case value matched %s." |> failwith
                            | x :: _ -> x

        let all =
            [|
                new OptionConverter() :> JsonConverter
                new TupleConverter() :> JsonConverter
                new UnionConverter() :> JsonConverter
            |]


    let tryParse = Option.tryFunction JsonValue.Parse
    let tryGet (property:string) (json:JsonValue) = json.TryGetProperty(property)
    let get (property:string) (json:JsonValue) = Option.get <| json.TryGetProperty(property)

    let properties (json:JsonValue) = json.Properties()
    let keys (json:JsonValue) = json.Properties() |> Array.map fst
    let values (json:JsonValue) = json.Properties() |> Array.map snd

    let toString (json:JsonValue) = json.ToString(JsonSaveOptions.DisableFormatting)
    let toBytes = toString >> fst Codec.StringToBytes
    let toType (json:JsonValue) : 'T = JsonConvert.DeserializeObject<'T>(toString json, Converters.all)

    let ofString (json:string) = JsonValue.Parse json
    let ofBytes = fst Codec.BytesToString >> ofString
    let ofType (t:'T) : JsonValue = JsonConvert.SerializeObject(t, Converters.all) |> ofString

    module Codec =

        let JsonValueToString : Codec<JsonValue,string> = toString, ofString
        let StringToJsonValue : Codec<string,JsonValue> = ofString, toString

        let JsonValueToBytes : Codec<JsonValue,byte[]> = toBytes, ofBytes
        let BytesToJsonValue : Codec<byte[],JsonValue> = ofBytes, toBytes

        let createJsonValueToType() : Codec<JsonValue,'T> = toType, ofType
        let createTypeToJsonValue() : Codec<'T,JsonValue> = ofType, toType

        let createTypeToString() = createTypeToJsonValue() |> Codec.concatenate JsonValueToString
        let createStringToType() = createTypeToString()    |> Codec.reverse

        let createTypeToBytes() = createTypeToJsonValue() |> Codec.concatenate JsonValueToBytes
        let createBytesToType() = createTypeToBytes()     |> Codec.reverse