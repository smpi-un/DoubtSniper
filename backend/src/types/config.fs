module Config
open Tomlyn
open Tomlyn.Model
open System.IO

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
