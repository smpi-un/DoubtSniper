module Common

open System
type GameType =
    | SelectDoubt
    | DoubtOrNoDoubts

let shuffle (rnd: Random) (list: 'a list) : 'a list =
    list 
    |> List.map (fun x -> (rnd.Next(), x)) // 各要素にランダムな数値を付ける
    |> List.sortBy fst                     // ランダム値でソート
    |> List.map snd                        // 元の要素だけを取り出す
