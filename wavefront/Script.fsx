#load "Values.fs"

open wavefront

let ab =
    Values.empty
    |> Values.set "A" 1
    |> Values.set "B" "bee"

ab.Get<int> "A"
ab.Get<string> "B"