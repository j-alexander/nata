namespace Nata.IO.WebSocket

open System
open System.Collections.Concurrent

type Multicast<'T>() =

    let publish, subscribe =
        let listeners = new ConcurrentDictionary<string,'T->unit>();

        (fun (x:'T) ->
            for listener in listeners.Values do listener x),

        (fun (x:'T->unit) ->
            let id = Guid.NewGuid().ToString()
            ignore (listeners.GetOrAdd(id, x))
            { new IDisposable with 
                member x.Dispose() =
                    ignore(listeners.TryRemove(id)) })

    member x.Subscribe() =
        seq {
            use buffer = new BlockingCollection<'T>(new ConcurrentQueue<'T>())
            use connection = subscribe buffer.Add

            yield! Seq.initInfinite(fun _ -> buffer.Take())
        }

    member x.Publish = publish