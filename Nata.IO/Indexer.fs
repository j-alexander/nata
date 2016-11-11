namespace Nata.IO

open Nata.Core

type Indexer<'Index> = Position<'Index> -> 'Index

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Indexer =

    let mapIndex ((encode,decode):Codec<'IndexIn,'IndexOut>)
                 (indexer:Indexer<'IndexOut>) : Indexer<'IndexIn> =
        Position.map encode >> 
            Position.applyMap decode (indexer >> decode)

    let map = mapIndex