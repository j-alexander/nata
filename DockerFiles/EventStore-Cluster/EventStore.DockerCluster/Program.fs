open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Reflection
open System.Diagnostics

[<EntryPoint>]
let main argv = 
    
    let retry = 1000
    let rec lookup entry = 
        let ips =
            try
                Dns.GetHostAddresses(entry)
                |> Seq.filter (fun x -> x.AddressFamily = AddressFamily.InterNetwork)
                |> Seq.map (fun x -> x.ToString())
                |> Seq.toList
            with _ -> []
        match ips with
        | [] ->
            printfn "Lookup failed for %s, retrying in %dms" entry retry
            Thread.Sleep(retry)
            lookup entry
        | ip :: _ ->
            printfn "Resolved %s for %s" ip entry
            ip

    let hostIp =
        Dns.GetHostName()
        |> lookup 

    let gossipSeed,seeds =
        argv
        |> Array.map (lookup >> sprintf "%s:2113")
        |> String.concat ","
        ,
        argv
        |> Array.length
        |> (+) 1
    
    let directory = AppDomain.CurrentDomain.BaseDirectory
    let file = new FileInfo(Path.Combine(directory,"EventStore.ClusterNode.exe"))
    let arguments =
        sprintf " --db /Data --log /Logs --int-ip %s --ext-ip %s --int-tcp-port=1111 --ext-tcp-port=1112 --int-http-port=2113 --ext-http-port=2114 --ext-http-prefixes http://+:2114/ --run-projections=all --cluster-size=%d --discover-via-dns=false --gossip-seed=%s"
        <| hostIp
        <| hostIp
        <| seeds
        <| gossipSeed


    if not file.Exists then
        printfn "Unable to find %s" file.FullName
    else
        printfn "Starting %s %s" file.FullName arguments

        let info = new ProcessStartInfo(file.FullName, arguments)
        info.UseShellExecute <- false
        info.RedirectStandardOutput <- true
        info.RedirectStandardError <- true
        
        let instance = Process.Start(info)
        instance.OutputDataReceived.Add(fun x -> Console.WriteLine(x.Data))
        instance.ErrorDataReceived.Add(fun x -> Console.Error.WriteLine(x.Data))
        instance.BeginOutputReadLine()
        instance.BeginErrorReadLine()
        instance.WaitForExit()
    0
