module Web

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

/// <summary>
/// Retrieves a question by its ID from a list of questions.
/// </summary>
/// <param name="id">The ID of the question to retrieve.</param>
/// <param name="questions">The list of questions to search.</param>
/// <returns>The question with the specified ID, or None if not found.</returns>
let getQuestionById (id: string) (questions: QuestionDef list) : QuestionDef option =
    questions |> List.tryFind (fun q -> q.id = id)

// 回答リクエストを処理する関数
let handleAnswerRequest (questions: QuestionDef list) (context: HttpContext) : Task =
  task {
    try
      let! answerRequest = context.Request.ReadFromJsonAsync<AnswerRequest>()
      printfn "handleAnswerRequest %s" (answerRequest.ToString())
      let request = answerRequest // answerRequest の値を request という名前でバインド
      // F#のレコード型はnullにならないため、nullチェックは不要
      // ReadFromJsonAsyncが失敗した場合はtry...withで捕捉される
      match getQuestionById request.questionId questions with
        | Some question ->
          let correctAnswerText = getCorrectText question
          // 問題のtextに{...|...}が含まれているか（WrongTextを期待しているか）を判定
          let expectsWrongText = not (question.text |> getSelection |> List.isEmpty)
          // ユーザーの回答 (isCorrect) と期待される回答タイプが一致するかで最終的な正誤を判定
          let isCorrect = (request.isCorrect = not expectsWrongText)

          let answerResponse = {
              isCorrect = isCorrect
              correctAnswer = correctAnswerText
              explanation = question.explanation
          }

          context.Response.StatusCode <- 200 // OK
          let! _ = context.Response.WriteAsJsonAsync(answerResponse)
          ()
        | None ->
          context.Response.StatusCode <- 404 // Not Found
          let! _ = context.Response.WriteAsync($"Question with ID {request.questionId} not found")
          ()
    with
    | ex ->
      context.Response.StatusCode <- 500 // Internal Server Error
      let! _ = context.Response.WriteAsync($"Internal server error: {ex.Message}")
      ()
  }
let handleQuestionsRequest  (questions: QuestionDef list) (context: HttpContext) : string =
  """{ "id": "a1b2c3d4-e5f6-7890-abcd-1234567890ab",  "text": "これはテスト問題です"  }"""
/// Webアプリケーションを構成する
let configureWebApp (questions: QuestionDef list) =
  let builder = WebApplication.CreateBuilder()

  // Add CORS services
  builder.Services.AddCors(fun options ->
    options.AddDefaultPolicy(
      System.Action<Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder>(fun builder ->
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader() |> ignore))) |> ignore

  let app = builder.Build()

  // Use CORS middleware
  app.UseCors()

  app.MapGet("/", Func<HttpContext, string>(fun _ -> "DoubtSniper Web API is running.")) |> ignore
  app.MapGet("/questions", Func<HttpContext, string>(fun _ -> questions |> List.map (fun q -> q.ToString()) |> String.concat "\n")) |> ignore
  app.MapGet("/exam", Func<HttpContext, string>(handleQuestionsRequest questions)) |> ignore
  // 回答を受け付け、結果を返す新しいエンドポイント
  app.MapPost("/answer", Func<HttpContext, Task>(handleAnswerRequest questions)) |> ignore

  app
