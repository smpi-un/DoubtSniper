open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Text.RegularExpressions
open DoubtSnaiper
open Config
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Json // JSON処理のために追加
open System.Threading.Tasks // Task 型のために追加
open Argu
open Web
open Cli
open Common


type CLIArguments =
    | [<SubCommand>] Server
    | [<SubCommand>] Game
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Server -> "Start the web server."
            | Game _ -> "Run the game with the specified type."


/// 質問データを読み込む
let loadQuestions (config: Config) =
    let boxPaths =
        config.Boxes
        |> List.collect (fun box -> box.Paths)

    let loadQuestionsFromFiles filePattern parser =
        boxPaths
        |> List.collect (fun boxPath ->
            Directory.GetFiles(boxPath, filePattern, SearchOption.AllDirectories)
            |> Seq.toList
            |> List.collect (Path.GetFullPath >> File.ReadAllText >> parser))

    let tomlQuestions = loadQuestionsFromFiles "*.toml" parseTomlQuestions
    let csvQuestions = loadQuestionsFromFiles "*.csv" parseCsvQuestions

    tomlQuestions @ csvQuestions


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


[<EntryPoint>]
let main argv =
    let parser: ArgumentParser<CLIArguments> = ArgumentParser.Create<CLIArguments>()
    let results = parser.Parse(argv)

    let config = readConfig "config.toml"
    let questions = loadQuestions config

    match results.GetAllResults() with
    | [ Server ] ->
        let app = configureWebApp questions
        app.Run()
        0
    | [ Game ] ->
        runCliQuiz questions DoubtOrNoDoubts
    | _ ->
        printfn "%s" (parser.PrintUsage())
        1