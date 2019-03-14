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

            let buildOpt select apply (b:StreamMetadataBuilder) =
                match select metadata with Some x -> apply b x | _ -> b
            let buildMap select apply =
                select metadata
                |> Map.foldBack (fun k v (b:StreamMetadataBuilder) -> apply b (k,v))

            let buildRoles =
                buildMap roles
                <|
                fun b ->
                    function
                    | (Role.Write, AppliesTo.Data), roles -> b.SetWriteRoles(roles)
                    | (Role.Read, AppliesTo.Data), roles -> b.SetReadRoles(roles)
                    | (Role.Delete, AppliesTo.Data), roles -> b.SetDeleteRoles(roles)
                    | (Role.Write, AppliesTo.Metadata), roles -> b.SetMetadataWriteRoles(roles)
                    | (Role.Read, AppliesTo.Metadata), roles -> b.SetMetadataReadRoles(roles)
                    | (Role.Delete, AppliesTo.Metadata), roles -> b
            let buildCustom =
                buildMap custom
                <|
                fun b (k:string,v:string) -> b.SetCustomProperty(k, v)

            let streamMetadataBuilder =
                StreamMetadata.Build()
                |> buildOpt maxCount (fun b x -> b.SetMaxCount(x))
                |> buildOpt maxAge (fun b x -> b.SetMaxAge(x))
                |> buildOpt cacheControl (fun b x -> b.SetCacheControl(x))
                |> buildOpt truncateBefore (fun b x -> b.SetTruncateBefore(x))
                |> buildRoles
                |> buildCustom
            let streamMetadata =
                streamMetadataBuilder.Build()
            connection.SetStreamMetadataAsync(stream, ExpectedVersion.Any, streamMetadata)
            |> Task.wait

        stream