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
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Server -> "Start the web server."
            | Game -> "Run the game with the specified type."




[<EntryPoint>]
let main argv =
    let parser: ArgumentParser<CLIArguments> = ArgumentParser.Create<CLIArguments>()
    let results = parser.Parse(argv)

    let config = readConfig "config.toml"
    let questions = loadQuestions config

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
        let app = configureWebApp questions
        app.Run()
        0
    | [ Game ] ->
        runCliQuiz questions DoubtOrNoDoubts
    | _ ->
        printfn "%s" (parser.PrintUsage())
        1