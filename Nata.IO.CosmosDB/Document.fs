namespace Nata.IO.CosmosDB

open System
open System.IO
open System.Text
open System.Net
open Microsoft.Azure.Cosmos
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Linq

open Nata.Core
open Nata.IO

module Document =

    type Version = string

    let (|ResourceNotFound|_|) : exn -> unit option=
        function
        | :? CosmosException as e
        | AggregateException (:? CosmosException as e)
            when e.StatusCode = HttpStatusCode.NotFound -> Some()
        | _ -> None

    let (|ResourceAlreadyExists|_|) : exn -> unit option=
        function
        | :? CosmosException as e
        | AggregateException (:? CosmosException as e)
            when e.StatusCode = HttpStatusCode.Conflict -> Some()
        | _ -> None

    let (|PreconditionNotMet|_|) : exn -> unit option=
        function
        | :? CosmosException as e
        | AggregateException (:? CosmosException as e)
            when e.StatusCode = HttpStatusCode.PreconditionFailed -> Some()
        | _ -> None
    

    type Metadata =
        { [<JsonProperty("id")>] id : string
          [<JsonProperty("_etag")>] _etag : string
          [<JsonProperty("_ts"); JsonConverter(typeof<UnixDateTimeConverter>)>] _ts : DateTime }
        
    module Metadata =

        let set (id:string) (bytes:byte[]) : byte[] =
            let json =
                bytes
                |> Encoding.UTF8.GetString
                |> JObject.Parse
            json.["id"] <- JToken.op_Implicit id
            json.ToString(Formatting.None)
            |> Encoding.UTF8.GetBytes
           
        let get (bytes:byte[]) : byte[]*Metadata =
            bytes,
            bytes
            |> Encoding.UTF8.GetString
            |> JsonConvert.DeserializeObject<Metadata>

    let connect : Container.Settings -> Source<Id,Bytes,Version> =
        fun container ->

            let container = Container.connect container

            fun (id:Id) ->
                let writeTo : WriterTo<Bytes,Version> =
                    fun position { Data=bytes } ->
                        let bytes =
                            bytes
                            |> Metadata.set id 
                        match position with
                        | Position.After (Position.At null)
                        | Position.At null
                        | Position.Start ->
                            use stream = new MemoryStream(bytes)
                            container.CreateItemStreamAsync(stream, new PartitionKey(id))
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response when response.IsSuccessStatusCode &&
                                                       response.StatusCode <> HttpStatusCode.PreconditionFailed ->
                                response.Headers.ETag
                            | Choice1Of2 _
                            | Choice2Of2 ResourceAlreadyExists -> raise (new Position.Invalid<_>(position))
                            | Choice2Of2 e -> Async.reraise(e)
                        | Position.After (Position.At etag)
                        | Position.At etag ->
                            use stream = new MemoryStream(bytes)
                            let options = new ItemRequestOptions(IfMatchEtag=etag)
                            container.ReplaceItemStreamAsync(stream, id, new PartitionKey(id), options)
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response when response.IsSuccessStatusCode &&
                                                       response.StatusCode <> HttpStatusCode.PreconditionFailed ->
                                response.Headers.ETag
                            | Choice1Of2 _
                            | Choice2Of2 ResourceNotFound
                            | Choice2Of2 PreconditionNotMet -> raise (new Position.Invalid<_>(position))
                            | Choice2Of2 e -> Async.reraise(e)
                        | Position.End ->
                            let response =
                                use stream = new MemoryStream(bytes)
                                container.UpsertItemStreamAsync(stream, new PartitionKey(id))
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                            response.Headers.ETag
                        | Position.Before _ ->
                            raise (new NotSupportedException("Position.Before is not supported."))
                        | Position.After _ ->
                            raise (new NotSupportedException("Position.After is not supported."))

                let write =
                    writeTo Position.End
                    >> ignore

                let rec readFrom position =
                    seq {
                        yield!
                            container.ReadItemStreamAsync(id,new PartitionKey(id))
                            |> Async.AwaitTask
                            |> Async.Catch
                            |> Async.RunSynchronously
                            |>
                            function
                            | Choice1Of2 response when response.IsSuccessStatusCode ->
                                let data, metadata =
                                    use stream = new MemoryStream()
                                    response.Content.CopyTo(stream)
                                    stream.ToArray()
                                    |> Metadata.get
                                seq {
                                    yield
                                        Event.create data
                                        |> Event.withName metadata.id
                                        |> Event.withCreatedAt metadata._ts
                                        |> Event.withTag metadata._etag,
                                        metadata._etag
                                    yield! readFrom position
                                }
                            | Choice1Of2 _
                            | Choice2Of2 ResourceNotFound -> Seq.empty
                            | Choice2Of2 e -> Async.reraise(e)
                    }

                let read() =
                    readFrom Position.End
                    |> Seq.map fst

                [
                    Nata.IO.Writer <| write

                    Nata.IO.WriterTo <| writeTo

                    Nata.IO.Reader <| read

                    Nata.IO.ReaderFrom <| readFrom
                ]
