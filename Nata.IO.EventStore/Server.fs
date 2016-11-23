namespace Nata.IO.EventStore

type Server = {
    Host : string
    Port : int
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Server =

    let host (server:Server) = server.Host
    let port (server:Server) = server.Port

    let localhost = { Host="localhost"
                      Port=1113 }