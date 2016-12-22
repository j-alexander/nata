namespace Nata.Core

module GZip =

    open System.IO
    open System.IO.Compression

    module Stream =
    
        let write (target : Stream) (lines : seq<string>) =
            use gzip = new GZipStream(target, CompressionLevel.Optimal)
            use writer = new StreamWriter(gzip)

            for line in lines do
                writer.WriteLine(line)
                writer.Flush()

        let read (source : Stream) : seq<string> =
            seq {
                use gzip = new GZipStream(source, CompressionMode.Decompress)
                use reader = new StreamReader(gzip)
                
                while not reader.EndOfStream do
                    yield reader.ReadLine() 
            }

    module File =

        let write (target:FileInfo) (lines:seq<string>) =
            use stream = File.OpenWrite(target.FullName)
            Stream.write stream lines

        let read (source:FileInfo) : seq<string> =
            seq {
                use stream = File.OpenRead(source.FullName)
                yield! Stream.read stream
            }