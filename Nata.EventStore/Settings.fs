namespace Nata.EventStore

type Settings = {
    Server : Server
    User : User
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Settings =

    let server (settings:Settings) = settings.Server
    let user (settings:Settings) = settings.User

    let defaultSettings = { Server=Server.localhost
                            User=User.defaultCredentials }