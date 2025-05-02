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
