namespace Nata.IO.EventStore

type User = {
    Name : string
    Password : string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module User =

    let name (user:User) = user.Name
    let password (user:User) = user.Password

    let defaultCredentials = { Name="admin"
                               Password="changeit" }