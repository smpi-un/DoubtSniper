open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Text.RegularExpressions
open DoubtSnaiper




type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts



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
/// <summary>
/// Asks a question and processes the user's answer.
/// </summary>
/// <param name="question">The question to ask.</param>
(*
let askQuestion (question: Question) (answer: Answer) : bool =
    printfn "Category: %s" question.category
    printfn "Name: %s" question.name
    printfn "Description: %s" question.description

    let finalText = replacePlaceholdersForSelectDoubt question.text question.locations answer
    printfn "Text: %s" finalText

    printf "Enter the number of the incorrect location: "
    let input = Console.ReadLine()

    // printf "Answer: %s" answer.location

    let answerIndex = 
      match answer with
      | WrongText ans -> ans.locationIndex.ToString()
      | CorrectText -> "0"
    let isCorrect = (input.Trim()) = answerIndex

    printfn (if isCorrect then "✔Correct!" else "✗Incorrect.")

    match answer with
    | WrongText ans -> printfn "Answer: (%s)%s" (ans.locationIndex.ToString()) ans.correctAnswer
    | CorrectText -> printfn "Answer: No incorrect location"
    printfn "%s" question.explanation
    isCorrect
*)

let shuffle list =
    let rnd = System.Random()
    list 
    |> List.map (fun x -> (rnd.Next(), x)) // 各要素にランダムな数値を付ける
    |> List.sortBy fst                     // ランダム値でソート
    |> List.map snd                        // 元の要素だけを取り出す

let listupToml dirPath =
    if not (Directory.Exists(dirPath)) then
        []
    else
        let files = Directory.GetFiles(dirPath, "*.toml", SearchOption.AllDirectories)
        files |> Seq.toList |> List.map Path.GetFullPath

/// <summary>
/// The main entry point for the application.
/// </summary>
/// <param name="argv">Command-line arguments.</param>
/// <returns>An integer exit code.</returns>
[<EntryPoint>]
let main argv =
    // DoubtSnaiper.main argv |> ignore
    // let filePaths = ["data/questions.toml"; "data/不正競争防止法.toml"]
    let dirPath = "data/chuushou"
    let gameType = DoubtOrNoDoubts
    let filePaths = listupToml dirPath
    let tomls = filePaths |> List.map File.ReadAllText
    let questions = tomls |> List.map parseQuestions |> List.concat

    let rnd = Random()
    let answers = questions |> List.map (fun q -> getRandomAnswer q rnd 0.5)

    let num = 10

    match gameType with
    | SelectDoubt ->
    (*
        let correctNum = List.zip questions answers
                         |> shuffle
                         |> List.truncate num
                         |> List.map (fun (q, a) -> askQuestion q a)
                         |> List.filter id
                         |> List.length
        printfn "Correct: %d/%d" correctNum (questions.Length)
        *)
        0
    | DoubtOrNoDoubts ->
        let correctNum = List.zip questions answers
                         |> shuffle
                         |> List.truncate num
                         |> List.map (fun (q, a) -> askQuestion2 q a)
                         |> List.filter id
                         |> List.length
        printfn "Correct: %d/%d" correctNum (Math.Min(num, questions.Length))
        0