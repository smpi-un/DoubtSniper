open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Text.RegularExpressions
open DoubtSnaiper


// モデル定義
type Location = {
    index: int
    placeholder: string
    wrong_options: string list
    correctAnswer: string
}


type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts

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
/// Parses a TOML file to extract a list of questions.
/// </summary>
/// <param name="filePath">The path to the TOML file.</param>
/// <returns>A list of questions parsed from the file.</returns>
let parseQuestions (toml: string) : Question list =
    /// <summary>
    /// Extracts the correct answer for a given location ID from the text.
    /// </summary>
    /// <param name="text">The text containing embedded locations.</param>
    /// <param name="locationId">The ID of the location to extract.</param>
    /// <returns>The correct answer for the specified location.</returns>
    let extractEmbeddedLocations (text: string, locationId: string) : string =
        let pattern = @"\{(?<loc>[^:]+):(?<correct>[^}]+)\}"
        let matches = Regex.Matches(text, pattern)
        matches
        |> Seq.cast<Match>
        |> Seq.filter (fun m -> m.Groups.["loc"].Value = locationId)
        |> Seq.mapi (fun idx m -> m.Groups.["correct"].Value)
        |> Seq.head

    let model = Toml.Parse(toml).ToModel()
    let questions = model.["questions"] :?> TomlTableArray

    /// <summary>
    /// Converts a TOML table array to a list of Location objects.
    /// </summary>
    /// <param name="table">The TOML table array containing location data.</param>
    /// <param name="text">The text associated with the locations.</param>
    /// <returns>A list of Location objects.</returns>
    let toLocationList (table: TomlTableArray) (text: string) =
        table
        |> Seq.toList
        |> withIndex 1
        |> List.map (fun (loc, idx) ->
            {
                index = idx
                placeholder = loc.["location"].ToString()
                wrong_options = (loc.["wrong_options"] :?> TomlArray) |> Seq.map (fun o -> o.ToString()) |> Seq.toList
                correctAnswer = extractEmbeddedLocations(text, loc.["location"].ToString())})
                // correctAnswer = loc.["correct_answer"].ToString() })

    questions
    |> Seq.map (fun q ->
        {
            id = q.["id"].ToString()
            name = if q.ContainsKey("name") then q.["name"].ToString() else ""
            category = if q.ContainsKey("category") then q.["category"].ToString() else ""
            description = if q.ContainsKey("description") then q.["description"].ToString() else ""
            text = q.["text"].ToString()
            explanation = if q.ContainsKey("explanation") then q.["explanation"].ToString() else ""
            locations =
              if q.ContainsKey("locations") then
                toLocationList (q.["locations"] :?> TomlTableArray) (q.["text"].ToString())
              else
                []
        })
    |> Seq.toList

/// <summary>
/// Replaces placeholders in a text with the corresponding answers.
/// </summary>
/// <param name="text">The text containing placeholders.</param>
/// <param name="locations">The list of locations with answers.</param>
/// <param name="answer">The answer to use for replacement.</param>
/// <returns>The text with placeholders replaced by answers.</returns>
let replacePlaceholdersForDoubtOrNoDoubts (text: string) (locations: Location list) (answer: Answer) : string =
    locations
    |> List.fold (fun acc loc ->
        let answer_text =
          match answer with
          | WrongText ans -> if loc.placeholder <> ans.location then loc.correctAnswer else ans.replaceText
          | CorrectText -> loc.correctAnswer
        let pattern = @"\{" + loc.placeholder + @":([^}]+)\}"
        let replacement = answer_text
        Regex.Replace(acc, pattern, replacement)
    ) text
/// <summary>
/// Replaces placeholders in a text with the corresponding answers.
/// </summary>
/// <param name="text">The text containing placeholders.</param>
/// <param name="locations">The list of locations with answers.</param>
/// <param name="answer">The answer to use for replacement.</param>
/// <returns>The text with placeholders replaced by answers.</returns>
let replacePlaceholdersForSelectDoubt (text: string) (locations: Location list) (answer: Answer) : string =
    locations
    |> List.fold (fun acc loc ->
        let answer_text =
          match answer with
          | WrongText ans -> if loc.placeholder <> ans.location then loc.correctAnswer else ans.replaceText
          | CorrectText -> loc.correctAnswer
        let pattern = @"\{" + loc.placeholder + @":([^}]+)\}"
        let replacement = "(" + loc.index.ToString() + ")" + "[" + answer_text + "]"
        Regex.Replace(acc, pattern, replacement)
    ) text

/// <summary>
/// Selects a random question from a list of questions.
/// </summary>
/// <param name="questions">The list of questions to choose from.</param>
/// <returns>A randomly selected question.</returns>
let getRandomQuestion (questions: Question list) (rnd: Random) : Question =
    questions.[rnd.Next(questions.Length)]

/// <summary>
/// Generates a random answer for a given question.
/// </summary>
/// <param name="question">The question to generate an answer for.</param>
/// <returns>A random answer, either correct or incorrect.</returns>
let getRandomAnswer (question: Question) (rnd: Random) : Answer =
    let locationIndex = rnd.Next(0, question.locations.Length + 1)
    if locationIndex = 0 then
        CorrectText
    else
        let location = question.locations.[locationIndex - 1]
        let answer = location.wrong_options.[rnd.Next(location.wrong_options.Length)]
        WrongText { location = location.placeholder; locationIndex = location.index; replaceText = answer; correctAnswer = location.correctAnswer }

/// <summary>
/// Retrieves a question by its ID from a list of questions.
/// </summary>
/// <param name="id">The ID of the question to retrieve.</param>
/// <param name="questions">The list of questions to search.</param>
/// <returns>The question with the specified ID, or None if not found.</returns>
let getQuestionById (id: string) (questions: Question list) : Question option =
    questions |> List.tryFind (fun q -> q.id = id)

/// <summary>
/// Asks a question and processes the user's answer.
/// </summary>
/// <param name="question">The question to ask.</param>
let askQuestion2 (question: Question) (answer: Answer) : bool =
    printfn "Category: %s" question.category
    printfn "Name: %s" question.name
    printfn "Description: %s" question.description

    let finalText = replacePlaceholdersForDoubtOrNoDoubts question.text question.locations answer
    printfn "Text: %s" finalText

    printf "Enter the number of the incorrect location: "
    let input = Console.ReadLine()

    // printf "Answer: %s" answer.location

    let isCorrect =
        match answer with 
        | WrongText _ -> input.Trim() <> ""
        | CorrectText -> input.Trim() = ""

    printfn (if isCorrect then "✔Correct!" else "✗Incorrect.")

    match answer with
    | WrongText ans -> printfn "Answer: (%s)%s" (ans.locationIndex.ToString()) ans.correctAnswer
    | CorrectText -> printfn "Answer: No incorrect location"
    printfn "%s" question.explanation
    isCorrect
/// <summary>
/// Asks a question and processes the user's answer.
/// </summary>
/// <param name="question">The question to ask.</param>
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

let listupToml dirPath =
    let files = Directory.GetFiles(dirPath, "*.toml", SearchOption.AllDirectories)
    files |> Seq.toList |> List.map Path.GetFullPath

let shuffle list =
    let rnd = System.Random()
    list 
    |> List.map (fun x -> (rnd.Next(), x)) // 各要素にランダムな数値を付ける
    |> List.sortBy fst                     // ランダム値でソート
    |> List.map snd                        // 元の要素だけを取り出す

/// <summary>
/// The main entry point for the application.
/// </summary>
/// <param name="argv">Command-line arguments.</param>
/// <returns>An integer exit code.</returns>
[<EntryPoint>]
let main argv =
    DoubtSnaiper.main argv
    // let filePaths = ["data/questions.toml"; "data/不正競争防止法.toml"]
    let dirPath = "data/keizai"
    let gameType = DoubtOrNoDoubts
    let filePaths = listupToml dirPath
    let tomls = filePaths |> List.map File.ReadAllText
    let questions_list = tomls |> List.map parseQuestions

    let questions = questions_list |> List.concat
    let rnd = Random()
    let answers = questions |> List.map (fun q -> getRandomAnswer q rnd)

    let num = 10

    match gameType with
    | SelectDoubt ->
        let correctNum = List.zip questions answers
                         |> shuffle
                         |> List.truncate num
                         |> List.map (fun (q, a) -> askQuestion q a)
                         |> List.filter id
                         |> List.length
        printfn "Correct: %d/%d" correctNum (questions.Length)
        0
    | DoubtOrNoDoubts ->
        let correctNum = List.zip questions answers
                         |> shuffle
                         |> List.truncate num
                         |> List.map (fun (q, a) -> askQuestion2 q a)
                         |> List.filter id
                         |> List.length
        printfn "Correct: %d/%d" correctNum (questions.Length)
        0