namespace Nata.IO.CosmosDB

open Nata.IO

module Document =

    let emulator =
        "https://localhost:8081",
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

    let connect : Source<'a,'b,'c> =
        fun _ ->
            [
            ]
