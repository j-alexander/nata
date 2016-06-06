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


let generate file =
    let template = Path.Combine(__SOURCE_DIRECTORY__, "Template.html")
    let source = Path.Combine(__SOURCE_DIRECTORY__, file |> sprintf "%s.md")
    let target = Path.Combine(__SOURCE_DIRECTORY__, file |> sprintf "output\%s.html")
    Literate.ProcessMarkdown(source, template, target)


generate "InstallEventStore"
generate "InstallKafka"


