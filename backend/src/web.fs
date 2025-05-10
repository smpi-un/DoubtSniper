module Web

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Config
open Question
open Common

[<AutoOpen>]
module StringExtnsions =
    module String =
        let truncate maxLength (str: string) =
            if String.IsNullOrEmpty(str) then
                str
            elif str.Length <= maxLength then
                str
            else
                str.Substring(0, maxLength) + "..."

/// ユーザーからの回答リクエストを表す型
type AnswerRequest = {
    questionId: string
    questionText: string
    isCorrect: bool // ユーザーが「正しい」か「間違い」かを選択した結果
}

/// 回答結果を返すための型
type AnswerResponse = {
    isCorrect: bool
    correctAnswer: string
    explanation: string
}
/// <summary>
/// Retrieves a question by its ID from a list of questions.
/// </summary>
/// <param name="id">The ID of the question to retrieve.</param>
/// <param name="questions">The list of questions to search.</param>
/// <returns>The question with the specified ID, or None if not found.</returns>
let getQuestionById (id: string) (questions: QuestionDef list): QuestionDef option =
    questions |> List.tryFind (fun q -> q.id = id)

// 回答リクエストを処理する関数
let handleAnswerRequest (questions: QuestionDef list) (context: HttpContext): Task =
    task {
        printfn "[DEBUG] Starting handleAnswerRequest"
        try
            printfn "[DEBUG] Reading request body..."
            // リクエストボディをメモリストリームにコピーして複数回読み取り可能にする
            use memoryStream = new MemoryStream()
            do! context.Request.Body.CopyToAsync(memoryStream)
            memoryStream.Position <- 0L
            
            // デバッグ用にリクエストの生データを出力
            use reader = new StreamReader(memoryStream)
            let! rawBody = reader.ReadToEndAsync()
            printfn "[DEBUG] Raw request body: %s" rawBody
            
            // ストリームをリセット
            memoryStream.Position <- 0L
            
            // JSONリクエストを非同期で読み込み、AnswerRequest 型にデシリアライズ
            printfn "[DEBUG] Attempting to deserialize request..."
            context.Request.Body <- memoryStream
            let! answerRequest = context.Request.ReadFromJsonAsync<AnswerRequest>()
            printfn "[DEBUG] Successfully deserialized request: %A" answerRequest
            printfn "[DEBUG] Total questions available: %d" questions.Length
            printfn "[DEBUG] First few question IDs: %A" (questions |> List.truncate 5 |> List.map (fun q -> q.id))
            
            //questionId を使って該当する質問を検索
            printfn "[DEBUG] Looking for question with ID: %s" answerRequest.questionId
            // printfn "[DEBUG] Question IDs in list: %A" (questions |> List.map (fun q -> q.id))
            match getQuestionById answerRequest.questionId questions with
            | Some question ->
                // 正しい回答テキストを取得
                let correctAnswerText = getCorrectText question
                // 問題テキストと正解テキストが一致するか（=誤答選択肢を期待するか）を判定
                let expectsWrongText = answerRequest.questionText = correctAnswerText
                // ユーザーの回答 (isCorrect) と期待される回答タイプが一致するかで最終的な正誤を判定
                // ユーザーが「正解」を選び(isCorrect=true)、かつ問題が正答を期待する場合(expectsWrongText=false) -> 正解
                // ユーザーが「不正解」を選び(isCorrect=false)、かつ問題が誤答を期待する場合(expectsWrongText=true) -> 正解
                let isFinalCorrect = (answerRequest.isCorrect = expectsWrongText)

                // クライアントへのレスポンスを作成
                let answerResponse =
                    {
                        isCorrect = isFinalCorrect
                        correctAnswer = correctAnswerText
                        explanation = question.explanation
                    }

                // HTTPステータスコード 200 (OK) を設定
                context.Response.StatusCode <- StatusCodes.Status200OK
                // レスポンスをJSON形式で非同期に書き込み
                do! context.Response.WriteAsJsonAsync(answerResponse)

            | None ->
                // 該当する質問が見つからない場合
                // HTTPステータスコード 404 (Not Found) を設定
                context.Response.StatusCode <- StatusCodes.Status404NotFound
                // エラーメッセージを非同期に書き込み
                do! context.Response.WriteAsync($"Question with ID {answerRequest.questionId} not found")

        with
        // JSONデシリアライズ失敗やその他の予期せぬエラーを捕捉
        | ex ->
            // エラーログを出力（実際のアプリケーションではより詳細なロギングを推奨）
            printfn "Error handling answer request: %s" ex.Message
            // HTTPステータスコード 500 (Internal Server Error) を設定
            context.Response.StatusCode <- StatusCodes.Status500InternalServerError
            // エラーメッセージを非同期に書き込み
            do! context.Response.WriteAsync($"Internal server error: {ex.Message}")
    }

// TODO: この関数は現在ハードコードされたJSONを返しています。
// 実際のユースケースに合わせて、questions リストから動的に試験問題を生成・選択し、
// 適切なJSON形式で返すように実装する必要があります。
let handleExamRequest (questions: QuestionDef list) (context: HttpContext): Task =
    // 仮のレスポンス (実際には questions リストから問題を選択・加工する)
    let rnd = Random()
    let examResponse =
        questions
        |> shuffle rnd
        |> List.tryHead
        |> Option.map (fun q -> 
            let answer = getRandomAnswer q rnd 0.5
            let questionText = getQuestionText q answer
            {|
                id = q.id // 元の質問のIDを使用
                text = questionText
                // 必要に応じて他のフィールド (選択肢など) を追加
            |}
        )
    match examResponse with
    | Some response ->
        context.Response.StatusCode <- StatusCodes.Status200OK
        context.Response.WriteAsJsonAsync(response)
    | None ->
        context.Response.StatusCode <- StatusCodes.Status404NotFound
        context.Response.WriteAsync("No questions available")

/// Webアプリケーションを構成し、実行する
let configureAndRunWebApp (questions: QuestionDef list) (args: string array) =
    let builder = WebApplication.CreateBuilder(args)
    
    // Cloud Run の PORT 環境変数を読み取り、そのポートでリッスンするように設定
    let port =
        match Environment.GetEnvironmentVariable("PORT") with
        | null | "" -> "8080" // PORT 環境変数が設定されていないか空の場合はデフォルトで 8080 を使用
        | s -> s
    builder.WebHost.UseUrls($"http://*:{port}") |> ignore

    // CORS (Cross-Origin Resource Sharing) サービスを追加
    // これにより、異なるオリジン (ドメイン、ポート) からのWebフロントエンドからのリクエストを許可
    builder.Services.AddCors(fun options ->
        options.AddDefaultPolicy(
            // Action<T> の型注釈を追加して曖昧さを解消
            Action<CorsPolicyBuilder>(fun policyBuilder ->
                policyBuilder.AllowAnyOrigin() // すべてのオリジンを許可 (開発用、本番環境では制限を推奨)
                    .AllowAnyMethod()          // すべてのHTTPメソッド (GET, POSTなど) を許可
                    .AllowAnyHeader()          // すべてのHTTPヘッダーを許可
                |> ignore // ignore は Action<T> の戻り値 (void) を破棄するために必要
            )
        )
    ) |> ignore // AddCors の戻り値 (IServiceCollection) を破棄

    let app = builder.Build()

    // CORSミドルウェアをパイプラインに追加
    // これにより、設定したCORSポリシーがリクエストに適用される
    app.UseCors() |> ignore

    // ルートエンドポイント ("/") - APIが動作していることを確認するための基本的な応答
    app.MapGet("/", fun (context: HttpContext) ->
        // Task<string> を返す代わりに直接文字列を書き込む
        context.Response.WriteAsync("DoubtSniper Web API is running.")
    ) |> ignore // MapGet の戻り値 (IEndpointConventionBuilder) を破棄

    // "/questions" エンドポイント - すべての質問のリストを返す (デバッグ用など)
    // TODO: 大量の質問がある場合、パフォーマンスに影響する可能性があるため注意
    // 必要であれば QuestionDef の ToString() を適切に実装するか、
    // DTO (Data Transfer Object) を使って必要な情報だけを返すようにする
    app.MapGet("/questions", fun (context: HttpContext) ->
        let questionsText =
            questions
            |> List.map (fun q -> sprintf "ID: %s, Text: %s" q.id (q.text |> String.truncate 50)) // ToString() の代わりに整形
            |> String.concat "\n"
        context.Response.ContentType <- "text/plain; charset=utf-8" // ContentType を指定
        context.Response.WriteAsync(questionsText)
    ) |> ignore

    // "/exam" エンドポイント - 試験問題を取得する (現在は仮実装)
    app.MapGet("/exam", Func<HttpContext, Task>(handleExamRequest questions)) |> ignore

    // "/answer" エンドポイント - ユーザーの回答を受け付け、結果を返す
    app.MapPost("/answer", Func<HttpContext, Task>(handleAnswerRequest questions)) |> ignore

    // アプリケーションを実行
    app.Run()

// アプリケーションのエントリーポイント (例)
// 実際の実行は、このモジュールを呼び出す別の場所で行われることを想定
// [<EntryPoint>]
// let main argv =
//     // 設定ファイルなどから questions をロードする処理 (仮)
//     let questions = Config.loadQuestions "path/to/questions.toml"