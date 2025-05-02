module Common

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
type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts

let shuffle (rnd: Random) list =
    list 
    |> List.map (fun x -> (rnd.Next(), x)) // 各要素にランダムな数値を付ける
    |> List.sortBy fst                     // ランダム値でソート
    |> List.map snd                        // 元の要素だけを取り出す
