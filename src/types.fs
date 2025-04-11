module DoubtSnaiper
open System
open System.IO
open Tomlyn
open Tomlyn.Model
open System.Text.RegularExpressions



type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts

type QuestionDef = {
    id: string
    name: string
    category: string
    description: string
    text: string
    explanation: string
}

type WrongAnswerData = {
    locationIndex: int
    replaceTextIndex: int
}
type Answer =
    | WrongText of WrongAnswerData
    | CorrectText

type Question = {
    questionDef: QuestionDef
    answer: Answer
}
    
    

/// <summary>
/// Parses a TOML file to extract a list of questions.
/// </summary>
/// <param name="filePath">The path to the TOML file.</param>
/// <returns>A list of questions parsed from the file.</returns>
let parseQuestions (toml: string) : QuestionDef list =
    /// <summary>
    /// Extracts the correct answer for a given location ID from the text.
    /// </summary>
    /// <param name="text">The text containing embedded locations.</param>
    /// <param name="locationId">The ID of the location to extract.</param>
    /// <returns>The correct answer for the specified location.</returns>

    let model = Toml.Parse(toml).ToModel()
    let questions = model.["questions"] :?> TomlTableArray


    questions
    |> Seq.map (fun q ->
        {
            id = q.["id"].ToString()
            name = if q.ContainsKey("name") then q.["name"].ToString() else ""
            category = if q.ContainsKey("category") then q.["category"].ToString() else ""
            description = if q.ContainsKey("description") then q.["description"].ToString() else ""
            text = q.["text"].ToString()
            explanation = if q.ContainsKey("explanation") then q.["explanation"].ToString() else ""
        })
    |> Seq.toList

/// <summary>
/// {A|B} の形式をカンマに置き換え、指定された形式の文字列リストを返す関数。
/// </summary>
/// <param name="text">入力テキスト</param>
/// <returns>分割された文字列のリスト</returns>
let getTemplate (text: string) : string list =
    let pattern = @"\{[^}]*\}" // {A|B} の形式にマッチ（中身全体を対象）
    let parts = Regex.Split(text, pattern) // {A|B} をカンマに置換
    
    parts |> Seq.toList

/// <summary>
/// {A|B;C} の形式を解析し、(A, [B;C]) のタプルリストを返す関数。
/// </summary>
/// <param name="text">入力テキスト</param>
/// <returns>(キー, オプションリスト) のタプルリスト</returns>
let getSelection (text: string) : (string * string list) list =
    let pattern = @"\{([^|]+)\|([^}]*)\}" // {A|B;C} の形式にマッチ
    let matches = Regex.Matches(text, pattern)
    
    matches
    |> Seq.cast<Match>
    |> Seq.map (fun m ->
        let key = m.Groups.[1].Value // | の前の部分 (A)
        let options = 
            m.Groups.[2].Value // | の後の部分 (B;C)
            |> fun s -> s.Split(';') // ; で分割
            |> Array.map (fun s -> s.Trim()) // 前後の空白を削除
            |> Array.toList
        (key, options)) // (A, [B;C]) のタプルを作成
    |> Seq.toList


let getRandomAnswer (question: QuestionDef) (rnd: Random) (correctRate: float) : Answer =
    let lastIndex = (question.text |> getSelection |> List.length)
    if lastIndex = 0 then
        CorrectText
    else
        if rnd.NextDouble() < correctRate then
            let locationIndex = rnd.Next(0, lastIndex)
            let location = question.text |> getSelection |> List.item locationIndex
            WrongText {
                locationIndex = locationIndex
                replaceTextIndex = rnd.Next(location |> snd |> List.length)
            }
        else
            CorrectText

let getCorrectText (s: QuestionDef) : string =
    let template = getTemplate s.text
    let selection = getSelection s.text

    let keys = selection |> List.map fst

    let rec tatami1 (texts1: string list) (texts2: string list) (acc: string) =
        match texts1 with
        | [] -> acc // 残りの要素がなければ蓄積した文字列を返す
        | head :: tail -> tatami2 tail texts2 (acc + head)
    and tatami2 (texts1: string list) (texts2: string list) (acc: string) =
        match texts2 with
        | [] -> tatami1 texts1 texts2 acc
        | head :: tail -> tatami1 texts1 tail (acc + head)
        

    tatami1 template keys ""


let getAnswerText (s: QuestionDef) (answer: Answer) =
    match answer with
    | CorrectText -> s |> getCorrectText
    | WrongText wrongAnswer ->
        let template = getTemplate s.text
        let selection = getSelection s.text
        let keys = selection |> List.map fst

        let rec tatami1 (texts1: string list) (selection: (string * string list) list ) (i: int) (acc: string) =
            match texts1 with
            | [] -> acc // 残りの要素がなければ蓄積した文字列を返す
            | head :: tail -> tatami2 tail selection i (acc + head)
        and tatami2 (texts1: string list) (selection: (string * string list) list ) (i: int) (acc: string) =
            match selection with
            | [] -> tatami1 texts1 selection (i+1) acc
            | (correct, wrong_options) :: tail ->
                let rep = if i = wrongAnswer.locationIndex then "[" + correct + "]" else correct
                tatami1 texts1 tail (i+1) (acc + rep)
        tatami1 template selection 0 ""

let getQuestionText (s: QuestionDef) (answer: Answer) =
    match answer with
    | CorrectText -> s |> getCorrectText
    | WrongText wrongAnswer ->
        let template = getTemplate s.text
        let selection = getSelection s.text
        let keys = selection |> List.map fst

        let rec tatami1 (texts1: string list) (selection: (string * string list) list ) (i: int) (acc: string) =
            match texts1 with
            | [] -> acc // 残りの要素がなければ蓄積した文字列を返す
            | head :: tail -> tatami2 tail selection i (acc + head)
        and tatami2 (texts1: string list) (selection: (string * string list) list ) (i: int) (acc: string) =
            match selection with
            | [] -> tatami1 texts1 selection (i+1) acc
            | (correct, wrong_options) :: tail ->
                let rep = if i = wrongAnswer.locationIndex then wrong_options.[wrongAnswer.replaceTextIndex] else correct
                tatami1 texts1 tail (i+1) (acc + rep)
        tatami1 template selection 0 ""
    
(*
let main argv =
    // let filePaths = ["data/questions.toml"; "data/不正競争防止法.toml"]
    let dirPath = "data/test"
    let filePaths = listupToml dirPath
    let tomls = filePaths |> List.map File.ReadAllText
    let questions_list = tomls |> List.map parseQuestions
    let questions = questions_list |> List.concat
    let rnd = Random()
    printfn "%A" questions
    printfn "%s" (getCorrectText questions.[1])
    printfn "%s" (getQuestionText questions.[1] (getRandomWrongAnswer  questions.[1] rnd))
    // printfn "%A" (replaceOptionsWithCommaAndSplit questions.[0].text)
    // printfn "%A" (parseOptionsToTupleList questions.[0].text)
    // let questions = questions_list |> List.concat
    // let rnd = Random()
    // let answers = questions |> List.map (fun q -> getRandomAnswer q rnd)

    let num = 10


    0
    *)