namespace Nata.IO.Tests

open System
open System.Reflection
open System.Text
open FSharp.Data
open NUnit.Framework

open Nata.Core
open Nata.IO
open Nata.IO.Capability

[<AbstractClass>]
type SourceTests() as x =

    let date = DateTime.Now
    let overlayEvent =
        Event.createAt date 1
        |> Event.withEventType "event_type"
        |> Event.withStream "stream"
    let underlayEvent =
        Event.createAt date "data:1"
        |> Event.withEventType "event_type"
        |> Event.withStream "stream"

    let event(fn) =
        [| 
            "case", JsonValue.String (x.GetType().Name + "." + fn)
            "at", JsonValue.String (DateTime.Now.ToString()) 
            "from", JsonValue.String (Assembly.GetExecutingAssembly().FullName)
        |]
        |> JsonValue.Record
        |> JsonValue.toBytes       
        |> Event.create
        |> Event.withEventType fn

    abstract member Connect : unit -> Source<string,string,int64>
    abstract member Channel : unit -> string

    member private x.Capabilities() = x.Channel() |> x.Connect()

    member private x.CreateUnderlayAndOverlay() =
        let dataCodec : Codec<string,int> =
            (fun (below:string) -> below.Substring(5) |> Int32.Parse),
            (fun (above:int) -> sprintf "data:%d" above)
        let indexCodec = Codec.Identity

        let underlying : Source<string,string,int64> = x.Connect()
        let overlaying : Source<string,int,int64> =
            Source.map Codec.Identity dataCodec indexCodec underlying
            
        let channel = guid()
        underlying channel, overlaying channel

    [<Test>]
    member x.OverlayWriteCanReadAtBothTest() =
        let underlay,overlay = x.CreateUnderlayAndOverlay()

        overlayEvent |> writer overlay
        
        Assert.AreEqual(overlayEvent, overlay |> read |> Seq.head)
        Assert.AreEqual(underlayEvent, underlay |> read |> Seq.head)
        Assert.AreEqual(overlayEvent, overlay |> subscribe |> Seq.head)
        Assert.AreEqual(underlayEvent, underlay |> subscribe |> Seq.head)

    [<Test>]
    member x.UnderlayWriteCanReadAtBothTest() =
        let underlay,overlay = x.CreateUnderlayAndOverlay()

        underlayEvent |> writer underlay

        Assert.AreEqual(overlayEvent, overlay |> read |> Seq.head)
        Assert.AreEqual(underlayEvent, underlay |> read |> Seq.head)
        Assert.AreEqual(overlayEvent, overlay |> subscribe |> Seq.head)
        Assert.AreEqual(underlayEvent, underlay |> subscribe |> Seq.head)