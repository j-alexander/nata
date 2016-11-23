namespace Nata.IO.EventStore

type Options = {
    BatchSize : int
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Options =

    let batchSize (options:Options) = options.BatchSize

    let defaultOptions = { BatchSize = 1000 }