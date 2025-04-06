open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Text.RegularExpressions

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
let parseQuestions (filePath: string) : Question list =
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

    let toml = File.ReadAllText(filePath)
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
            name = q.["name"].ToString()
            category = q.["category"].ToString()
            description = q.["description"].ToString()
            text = q.["text"].ToString()
            explanation = q.["explanation"].ToString()
            locations = toLocationList (q.["locations"] :?> TomlTableArray) (q.["text"].ToString())
        })
    |> Seq.toList

/// <summary>
/// Replaces placeholders in a text with the corresponding answers.
/// </summary>
/// <param name="text">The text containing placeholders.</param>
/// <param name="locations">The list of locations with answers.</param>
/// <param name="answer">The answer to use for replacement.</param>
/// <returns>The text with placeholders replaced by answers.</returns>
let replacePlaceholders (text: string) (locations: Location list) (answer: Answer) : string =
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
let getRandomQuestion (questions: Question list) : Question =
    let rnd = Random()
    questions.[rnd.Next(questions.Length)]

/// <summary>
/// Generates a random answer for a given question.
/// </summary>
/// <param name="question">The question to generate an answer for.</param>
/// <returns>A random answer, either correct or incorrect.</returns>
let getRandomAnswer (question: Question) : Answer =
    let rnd = Random()
    let locationIndex = rnd.Next(0, question.locations.Length)
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
/// <summary>
/// The main entry point for the application.
/// </summary>
/// <param name="argv">Command-line arguments.</param>
/// <returns>An integer exit code.</returns>
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
