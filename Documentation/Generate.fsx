#r "System.Core.dll"
#r "System.dll"
#r "System.Numerics.dll"

#r "../packages/FSharp.Formatting.2.14.4/lib/net40/RazorEngine.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/System.Web.Razor.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/CSharpFormat.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/FSharp.CodeFormat.dll"
#r "../packages/FSharp.Compiler.Service.2.0.0.6/lib/net45/FSharp.Compiler.Service.dll"
#r "../packages/FSharpVSPowerTools.Core.2.3.0/lib/net45/FSharpVSPowerTools.Core.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/FSharp.Formatting.Common.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/FSharp.MetadataFormat.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/FSharp.Markdown.dll"
#r "../packages/FSharp.Formatting.2.14.4/lib/net40/FSharp.Literate.dll"

open System
open System.IO
open System.Reflection
open FSharp.Markdown
open FSharp.Literate


let output =
    let path = Path.Combine(__SOURCE_DIRECTORY__, "..", "gh-pages")
    let directory = new DirectoryInfo(path)
    if not directory.Exists then
        directory.Create()
    path

let copy folder file =
    let input = Path.Combine(__SOURCE_DIRECTORY__, folder, file)
    let output = Path.Combine(output, folder, file)
    let file = new FileInfo(output)
    if not file.Directory.Exists then
        file.Directory.Create()
    File.Copy(input, output, true)
    
let generateAs source target =
    let template = Path.Combine(__SOURCE_DIRECTORY__, "Template.html")
    let source = Path.Combine(__SOURCE_DIRECTORY__, source)
    let target = Path.Combine(output, target)
    Literate.ProcessMarkdown(source, template, target, generateAnchors=true)

let generate file =
    generateAs (file |> sprintf "%s.md") (file |> sprintf "%s.html")


copy "content" "style.css"
copy "content" "tips.js"

generate "InstallEventStore"
generate "InstallKafka"
generateAs "Index.md" "index.html"
