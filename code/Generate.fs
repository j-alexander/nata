namespace Code

open System.IO
open FSharp.Literate

module Program =

    let output =
        let path = Path.Combine(__SOURCE_DIRECTORY__, "..")
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
    
    let generateMarkdownAs source target =
        let template = Path.Combine(__SOURCE_DIRECTORY__, "Template.html")
        let source = Path.Combine(__SOURCE_DIRECTORY__, source)
        let target = Path.Combine(output, target)
        Literate.ProcessMarkdown(source, template, target, generateAnchors=true)

    let generateScriptAs source target =
        let template = Path.Combine(__SOURCE_DIRECTORY__, "Template.html")
        let source = Path.Combine(__SOURCE_DIRECTORY__, source)
        let target = Path.Combine(output, target)
        Literate.ProcessScriptFile(source, template, target, generateAnchors=true, lineNumbers=false)

    let generateMarkdown file =
        generateMarkdownAs (file |> sprintf "%s.md") (file |> sprintf "%s.html")
    
    let generateScript file =
        generateScriptAs (file |> sprintf "%s.fsx") (file |> sprintf "%s.html")

    [<EntryPoint>]
    let main(args:string array) =

        copy "content" "style.css"
        copy "content" "tips.js"
        
        generateScript "Nata.Core"
        generateMarkdownAs "Index.md" "index.html"

        0