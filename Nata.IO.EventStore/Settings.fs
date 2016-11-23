namespace Nata.IO.EventStore

type Settings = {
    Server : Server
    User : User
    Options : Options
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Settings =

    let server (settings:Settings) = settings.Server
    let user (settings:Settings) = settings.User
    let options (settings:Settings) = settings.Options

    let defaultSettings = { Server=Server.localhost
                            User=User.defaultCredentials
                            Options=Options.defaultOptions}