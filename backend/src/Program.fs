open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Argu
open Web
open Cli
open Config
open Question
open Common


type CLIArguments =
    | [<SubCommand>] Server
    | [<SubCommand>] Game
    | [<SubCommand>] GenerateUuid
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Server -> "Start the web server."
            | Game -> "Run the game with the specified type."
            | GenerateUuid -> "Generate UUIDs for items without ID in TOML files specified in config.toml."




[<EntryPoint>]
let main argv =
    let parser: ArgumentParser<CLIArguments> = ArgumentParser.Create<CLIArguments>()
    let results = parser.Parse(argv)

    let config = readConfig "config.toml"

    let configureWebApp (questions: QuestionDef list) =
        let builder = WebApplication.CreateBuilder()
        let app = builder.Build()
        
        // Configure CORS
        app.UseCors(fun policy ->
            policy.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 |> ignore
        ) |> ignore
        
        // Configure routes
        Web.configureAndRunWebApp questions argv
        
        app

    match results.GetAllResults() with
    | [ Server ] ->
        let questions = loadQuestionsFromConfig config
        let app = configureWebApp questions
        app.Run()
        0
    | [ Game ] ->
        let questions = loadQuestionsFromConfig config
        runCliQuiz questions DoubtOrNoDoubts |> ignore
        0
    | [ GenerateUuid ] ->
        printfn "Generating UUIDs for TOML files specified in config.toml..."
        config.Boxes
        |> List.iter (fun box ->
            let questions = loadQuestionsFromBox box
            printfn "Processing box: %s" (questions |> List.map (fun q -> q.id) |> String.concat " ")
            // Process paths within the box
            box.Paths
            |> List.iter (fun filePath ->
                printfn "Processing path: %s" filePath
                if System.IO.Directory.Exists(filePath) then
                    printfn "  Path is a directory. Searching for TOML files..."
                    System.IO.Directory.EnumerateFiles(filePath, "*.toml", SearchOption.AllDirectories)
                    |> Seq.iter (fun tomlFilePath ->
                        printfn "    Found TOML file: %s" tomlFilePath
                        Config.generateUuidsForFile tomlFilePath
                    )
                elif System.IO.File.Exists(filePath) then
                    printfn "  Path is a file. Generating UUIDs..."
                    Config.generateUuidsForFile filePath
                else
                    printfn "  Path does not exist. Skipping."
            )
        )
        0 // Indicate success
    | _ ->
        printfn "%s" (parser.PrintUsage())
        1