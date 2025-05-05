module Config
open Tomlyn
open Tomlyn.Model
open System.IO
open System // Added for System.Guid

type Box = {
    Name: string
    Paths: string list
}

type Config = {
    Boxes: Box list
}

let readConfig (filePath: string) : Config =
    let tomlContent = File.ReadAllText(filePath)
    let model = Toml.Parse(tomlContent).ToModel()
    let boxes = 
        model.["boxes"] :?> TomlTableArray
        |> Seq.map (fun box -> 
            { Name = box.["name"].ToString()
              Paths = (box.["paths"] :?> TomlArray) |> Seq.map (fun p -> p.ToString()) |> List.ofSeq })
        |> List.ofSeq
    { Boxes = boxes }

let generateUuidsForFile (filePath: string) =
    try
        let lines = File.ReadAllLines(filePath) |> List.ofArray
        let newLines =
            lines
            |> List.map (fun line ->
                let trimmedLine = line.Trim()
                // Check if the line matches the pattern 'id = "..."' allowing for flexible spacing
                let idRegex = System.Text.RegularExpressions.Regex(@"^\s*id\s*=\s*""(.*?)""\s*$")
                let idMatch = idRegex.Match(line)

                if idMatch.Success then
                    let currentId = idMatch.Groups.[1].Value
                    let isGuid =
                        match System.Guid.TryParse(currentId) with
                        | true, _ -> true
                        | false, _ -> false

                    if String.IsNullOrEmpty(currentId) || not isGuid then
                        let newUuid = System.Guid.NewGuid().ToString()
                        printfn "  Found invalid or empty ID '%s' in %s. Generating UUID: %s" currentId filePath newUuid
                        // Replace the entire line with the new ID line, preserving original indentation if possible
                        let originalIndent = line.Substring(0, line.IndexOf(trimmedLine))
                        $"{originalIndent}id = \"{newUuid}\""
                    else
                        line // Keep the original line if ID is a valid non-empty GUID
                else
                    line // Keep the original line if it doesn't match the id pattern
            )
        
        File.WriteAllLines(filePath, newLines |> Seq.toArray)
        printfn "Successfully processed %s" filePath

    with
    | ex ->
        eprintfn "Error processing file %s: %s" filePath ex.Message
