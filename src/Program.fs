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
open Argu

type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts

type CLIArguments =
    | [<SubCommand>] Server
    | [<SubCommand>] Game
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Server -> "Start the web server."
            | Game _ -> "Run the game with the specified type."



/// <summary>
/// Adds an index to each element of a list, starting from a given number.
/// </summary>
/// <param name="start">The starting index number.</param>
/// <param name="list">The list to index.</param>
/// <returns>A list of tuples, each containing an element and its index.</returns>
let rec withIndex (start: int) (list: 'a list) : ('a * int) list =
  match list with
    | [] -> []
    | head :: tail -> (head, start) :: withIndex (start + 1) tail


/// <summary>
/// Selects a random question from a list of questions.
/// </summary>
/// <param name="questions">The list of questions to choose from.</param>
/// <returns>A randomly selected question.</returns>
let getRandomQuestion (questions: QuestionDef list) (rnd: Random) : QuestionDef =
    questions.[rnd.Next(questions.Length)]

/// <summary>
/// Retrieves a question by its ID from a list of questions.
/// </summary>
/// <param name="id">The ID of the question to retrieve.</param>
/// <param name="questions">The list of questions to search.</param>
/// <returns>The question with the specified ID, or None if not found.</returns>
let getQuestionById (id: string) (questions: QuestionDef list) : QuestionDef option =
    questions |> List.tryFind (fun q -> q.id = id)

/// <summary>
/// Asks a question and processes the user's answer.
/// </summary>
/// <param name="question">The question to ask.</param>
let askQuestion2 (question: QuestionDef) (answer: Answer) : bool =
    printfn "============================================"
    printfn ""
    if question.category <> "" then printfn "[ Category  ] %s" question.category
    if question.name <> "" then printfn "[   Name    ] %s" question.name
    if question.description <> "" then printfn "[Description] %s" question.description

    let finalText = getQuestionText question answer
    printfn "Q: %s" finalText

    printf "Enter the any character if there is incorrect location, otherwise enter nothing: "
    let input = Console.ReadLine()

    // printf "Answer: %s" answer.location

    let isCorrect =
        match answer with 
        | WrongText _ -> input.Trim() <> ""
        | CorrectText -> input.Trim() = ""

    printfn (if isCorrect then "☆★☆★☆★☆★ ✔Correct! ☆★☆★☆★☆★" else "☹☹☹☹☹☹☹☹ ✗Incorrect. ☹☹☹☹☹☹☹☹")

    match answer with
    | WrongText ans -> printfn "A: %s" (getAnswerText question answer)
    | CorrectText -> printfn "No incorrect location"

    let explanation = question.explanation.Split('\n') |> Seq.map (fun s -> "> " + s) |> String.concat "\n"
    if question.explanation <> "" then printfn "%s" explanation

    printfn ""

    isCorrect
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

/// Webアプリケーションを構成する
let configureWebApp (questions: QuestionDef list) =
    let builder = WebApplication.CreateBuilder()
    let app = builder.Build()

    app.MapGet("/", Func<HttpContext, string>(fun _ -> "DoubtSniper Web API is running."))
    app.MapGet("/questions", Func<HttpContext, string>(fun _ -> questions |> List.map (fun q -> q.ToString()) |> String.concat "\n"))

    app

let shuffle (rnd: Random) list =
    list 
    |> List.map (fun x -> (rnd.Next(), x)) // 各要素にランダムな数値を付ける
    |> List.sortBy fst                     // ランダム値でソート
    |> List.map snd                        // 元の要素だけを取り出す

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

/// ゲームを実行する
let runGame (questions: QuestionDef list) (gameType: GameType) =
    let rnd = Random()
    let answers = questions |> List.map (fun q -> getRandomAnswer q rnd 0.5)
    let num = 10

    match gameType with
    | SelectDoubt -> 
        printfn "SelectDoubt mode is not implemented yet."
        0
    | DoubtOrNoDoubts ->
        let correctNum = List.zip questions answers
                         |> shuffle rnd
                         |> List.truncate num
                         |> List.map (fun (q, a) -> askQuestion2 q a)
                         |> List.filter id
                         |> List.length
        printfn "Correct: %d/%d" correctNum (Math.Min(num, questions.Length))
        0

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
        runGame questions DoubtOrNoDoubts
    | _ ->
        printfn "%s" (parser.PrintUsage())
        1