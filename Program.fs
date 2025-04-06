open System
open System.IO
open Tomlyn
open Tomlyn.Model

// モデル定義
type Location = { index: int; placeholder: string; wrong_options: string list; correctAnswer: string }

type Question = {
    id: string
    name: string
    category: string
    description: string
    text: string
    explanation: string
    locations: Location list
}
type WrongAnswerData = {
    location: string
    locationIndex: int
    replaceText: string
    correctAnswer: string
}

type Answer =
    | WrongText of WrongAnswerData
    | CorrectText


let rec withIndex (start: int) (list: 'a list) : ('a * int) list =
  match list with
    | [] -> []
    | head :: tail -> (head, start) :: withIndex (start + 1) tail

// TOMLパース
let parseQuestions (filePath: string) : Question list =
    let toml = File.ReadAllText(filePath)
    let model = Toml.Parse(toml).ToModel()
    let questions = model.["questions"] :?> TomlTableArray

    let toLocationList (table: TomlTableArray) =
        table
        |> Seq.toList
        |> withIndex 1
        |> List.map (fun (loc, idx) ->
            {
                index = idx
                placeholder = loc.["location"].ToString()
                wrong_options = (loc.["wrong_options"] :?> TomlArray) |> Seq.map (fun o -> o.ToString()) |> Seq.toList
                correctAnswer = loc.["correct_answer"].ToString() })

    questions
    |> Seq.map (fun q ->
        {
            id = q.["id"].ToString()
            name = q.["name"].ToString()
            category = q.["category"].ToString()
            description = q.["description"].ToString()
            text = q.["text"].ToString()
            explanation = q.["explanation"].ToString()
            locations = toLocationList (q.["locations"] :?> TomlTableArray)
        })
    |> Seq.toList

// プレースホルダを置換
let replacePlaceholders (text: string) (locations: Location list) (answer: Answer) : string =
    locations
    |> List.fold (fun acc loc ->
        let answer_text =
          match answer with
          | WrongText ans -> if loc.placeholder <> ans.location then loc.correctAnswer else ans.replaceText
          | CorrectText -> loc.correctAnswer
        acc.Replace("{" + loc.placeholder + "}", "(" + loc.index.ToString() + ")" + "[" + answer_text + "]")
    ) text

// クイズ選択
let getRandomQuestion (questions: Question list) : Question =
    let rnd = Random()
    questions.[rnd.Next(questions.Length)]

let getRandomAnswer (question: Question) : Answer =
    let rnd = Random()
    let locationIndex = rnd.Next(0, question.locations.Length)
    if locationIndex = 0 then
        CorrectText
    else
        let location = question.locations.[locationIndex - 1]
        let answer = location.wrong_options.[rnd.Next(location.wrong_options.Length)]
        WrongText { location = location.placeholder; locationIndex = location.index; replaceText = answer; correctAnswer = location.correctAnswer }

let getQuestionById (id: string) (questions: Question list) : Question option =
    questions |> List.tryFind (fun q -> q.id = id)

let askQuestion (question: Question) : unit =
    printfn "Category: %s" question.category
    printfn "Name: %s" question.name
    printfn "Description: %s" question.description

    let answer = getRandomAnswer question
    let finalText = replacePlaceholders question.text question.locations answer
    printfn "Text: %s" finalText

    printf "Enter the number of the incorrect location: "
    let input = Console.ReadLine()

    // printf "Answer: %s" answer.location

    let answerIndex = 
      match answer with
      | WrongText ans -> ans.locationIndex.ToString()
      | CorrectText -> "0"
    let isCorrect = (input.Trim()) = answerIndex

    if isCorrect then
        printfn "✔Correct!"
    else
        printfn "✗Incorrect."
    match answer with
    | WrongText ans -> printfn "Answer: (%s)%s" (ans.locationIndex.ToString()) ans.correctAnswer
    | CorrectText -> printfn "Answer: No incorrect location"
    printfn "%s" question.explanation

// Elm風 main：状態初期化＋副作用まとめ
[<EntryPoint>]
let main argv =
    let questions = parseQuestions "data/questions.toml"

    let question =
        match argv |> Array.tryHead with
        | Some id -> getQuestionById id questions |> Option.defaultWith (fun () ->
                        printfn "Question ID not found. Selecting random question."; getRandomQuestion questions)
        | None -> getRandomQuestion questions

    askQuestion question
    0
