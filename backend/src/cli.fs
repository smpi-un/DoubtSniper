module Cli
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
open Common



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
/// ゲームを実行する
let runCliQuiz (questions: QuestionDef list) (gameType: GameType) =
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
