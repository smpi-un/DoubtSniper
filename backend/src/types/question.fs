module Question

open System
open System.IO
open System.Text.RegularExpressions
open Tomlyn
open Tomlyn.Model

open Config



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
/// <param name="toml">The TOML content as a string.</param>
/// <returns>A list of questions parsed from the file.</returns>
let parseTomlQuestions (toml: string) : QuestionDef list =
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
            id = q.["id"].ToString().Trim()
            name = if q.ContainsKey("name") then q.["name"].ToString().Trim() else ""
            category = if q.ContainsKey("category") then q.["category"].ToString().Trim() else ""
            description = if q.ContainsKey("description") then q.["description"].ToString().Trim() else ""
            text = q.["text"].ToString().Trim()
            explanation = if q.ContainsKey("explanation") then q.["explanation"].ToString().Trim() else ""
        })
    |> Seq.toList

/// <summary>
/// Parses a CSV file to extract a list of questions, handling arbitrary header order.
/// </summary>
/// <param name="csv">The CSV content as a string.</param>
/// <returns>A list of questions parsed from the file.</returns>
let parseCsvQuestions (csv: string) : QuestionDef list =
    // CSVを文字列の行ごとに分割
    let lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries) |> Array.toList
    
    // ヘッダー行とデータ行を分離
    match lines with
    | header :: dataLines ->
        // ヘッダー行をカンマで分割してトリム
        let headers = header.Split(',', StringSplitOptions.None) |> Array.map (fun s -> s.Trim().ToLower())
        
        // 各列のインデックスを検索
        let findIndex (name: string) = 
            Array.tryFindIndex (fun h -> h = name.ToLower()) headers
            |> Option.defaultValue -1 // 見つからない場合は-1

        let idIndex = findIndex "id"
        let nameIndex = findIndex "name"
        let categoryIndex = findIndex "category"
        let descriptionIndex = findIndex "description"
        let textIndex = findIndex "text"
        let explanationIndex = findIndex "explanation"

        // データ行をQuestionDefに変換
        dataLines
        |> List.choose (fun line ->
            try
                let columns = line.Split(',', StringSplitOptions.None) |> Array.map (fun s -> s.Trim())

                // 列インデックスが有効かチェックし、値を取得
                let getColumn index defaultValue =
                    if index >= 0 && index < columns.Length then columns.[index]
                    else defaultValue

                Some {
                    id = getColumn idIndex ""
                    name = getColumn nameIndex ""
                    category = getColumn categoryIndex ""
                    description = getColumn descriptionIndex ""
                    text = getColumn textIndex ""
                    explanation = getColumn explanationIndex ""
                }
            with
            | _ -> None // 行の解析に失敗した場合はスキップ
        )
    | [] -> []

let loadQuestionsFromFilePath (path: string) : QuestionDef list =
    let ext = Path.GetExtension(path).ToLower()
    match ext with
    | ".toml" -> parseTomlQuestions (File.ReadAllText(path))
    | ".csv" -> parseCsvQuestions (File.ReadAllText(path))
    | _ -> 
        // 他の拡張子は無視
        []

let loadQuestionsFromBox (box: Box) : QuestionDef list =
    box.Paths
    |> List.collect (fun boxPath ->
        Directory.GetFiles(boxPath, "*.toml", SearchOption.AllDirectories)
        |> Seq.toList)
    |> List.collect loadQuestionsFromFilePath

/// 質問データを読み込む
let loadQuestionsFromConfig (config: Config) =
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

    // 無効なIDを持つ質問を除外
    let validQuestions = 
        (tomlQuestions @ csvQuestions)
        |> List.filter (fun q -> not (String.IsNullOrWhiteSpace(q.id)))
    
    printfn "[DEBUG] Loaded %d valid questions (filtered from %d total)" validQuestions.Length (tomlQuestions.Length + csvQuestions.Length)
    validQuestions


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
    