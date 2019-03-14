namespace Nata.IO.EventStore

open System
open Nata.Core
open EventStore.ClientAPI

type Metadata = {
    MaxCount : int64 option
    MaxAge : TimeSpan option
    CacheControl : TimeSpan option
    TruncateBefore : int64 option
    Roles : Map<Role*AppliesTo, string[]>
    Custom : Map<string,string>
}
and [<RequireQualifiedAccess>] Role = Read | Write | Delete
and [<RequireQualifiedAccess>] AppliesTo = Data | Metadata

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Metadata =

    let maxCount { Metadata.MaxCount=x } = x
    let maxAge { Metadata.MaxAge=x } = x
    let cacheControl { Metadata.CacheControl=x } = x
    let truncateBefore { Metadata.TruncateBefore=x } = x
    let roles { Metadata.Roles=x } = x
    let custom { Metadata.Custom=x } = x

    let isEmpty (x:Metadata) =
        x.MaxCount.IsNone &&
        x.MaxAge.IsNone &&
        x.CacheControl.IsNone &&
        x.TruncateBefore.IsNone &&
        x.Roles.IsEmpty &&
        x.Custom.IsEmpty

    let empty : Metadata =
        { MaxCount=None
          MaxAge=None
          CacheControl=None
          TruncateBefore=None
          Roles=Map.empty
          Custom=Map.empty }

    let set (connection:IEventStoreConnection) (stream, metadata:Metadata) =
        if not (isEmpty metadata) then
            let rec applyRoles = function
                | [] -> id
                | x :: xs ->
                    fun (b:StreamMetadataBuilder) ->
                        match x with
                        | (Role.Write, AppliesTo.Data), roles -> b.SetWriteRoles(roles)
                        | (Role.Read, AppliesTo.Data), roles -> b.SetReadRoles(roles)
                        | (Role.Delete, AppliesTo.Data), roles -> b.SetDeleteRoles(roles)
                        | (Role.Write, AppliesTo.Metadata), roles -> b.SetMetadataWriteRoles(roles)
                        | (Role.Read, AppliesTo.Metadata), roles -> b.SetMetadataReadRoles(roles)
                        | (Role.Delete, AppliesTo.Metadata), roles -> b
                        |> applyRoles xs
            let rec applyCustom : List<_*string>->_->_ = function
                | [] -> id
                | (k,v) :: xs ->
                    fun (b:StreamMetadataBuilder) ->
                        b.SetCustomProperty(k,v)
                        |> applyCustom xs
            let streamMetadata =
                StreamMetadata.Build()
                |> match metadata.MaxCount with Some x -> (fun b -> b.SetMaxCount(x)) | _ -> id
                |> match metadata.MaxAge with Some x -> (fun b -> b.SetMaxAge(x)) | _ -> id
                |> match metadata.CacheControl with Some x -> (fun b -> b.SetCacheControl(x)) | _ -> id
                |> match metadata.TruncateBefore with Some x -> (fun b -> b.SetTruncateBefore(x)) | _ -> id
                |> applyRoles (Map.toList metadata.Roles)
                |> applyCustom (Map.toList metadata.Custom)
                |> fun b -> b.Build()
            connection.SetStreamMetadataAsync(stream, ExpectedVersion.Any, streamMetadata)
            |> Task.wait
        stream