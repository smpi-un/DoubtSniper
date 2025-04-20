module Main exposing (main)

import Browser
import Element exposing (Element)
import Element.Input as Input
import Http
import Json.Decode as Decode
import Url
import Widget exposing (column)
import Widget as Button
import Widget.Material as Material
--import Widget as TextField
import Json.Encode

-- Model

type alias Question =
    { id : String
    , text : String
    }

type QuestionState
    = Loading
    | Success Question
    | Failure Http.Error

type alias Model =
    { questionState : QuestionState
    , answerInput : String
    }

initialModel : Model
initialModel =
    { questionState = Loading
    , answerInput = ""
    }

-- Msg

type Msg
    = FetchQuestion
    | FetchQuestionComplete (Result Http.Error Question)
    | AnswerInput String
    | SubmitAnswer
    | SubmitAnswerComplete (Result Http.Error ())

-- Update

update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        FetchQuestion ->
            ( { model | questionState = Loading }
            , fetchQuestion
            )

        FetchQuestionComplete result ->
            case result of
                Ok question ->
                    ( { model | questionState = Success question }
                    , Cmd.none
                    )
                Err error ->
                    ( { model | questionState = Failure error }
                    , Cmd.none
                    )

        AnswerInput input ->
            ( { model | answerInput = input }
            , Cmd.none
            )

        SubmitAnswer ->
            case model.questionState of
                Success question ->
                    ( model
                    , submitAnswer question.id model.answerInput
                    )
                _ ->
                    ( model
                    , Cmd.none
                    )

        SubmitAnswerComplete result ->
            case result of
                Ok () ->
                    ( model
                    , Cmd.none
                    )
                Err error ->
                    ( model
                    , Cmd.none
                    )

-- HTTP

fetchQuestion : Cmd Msg
fetchQuestion =
    Http.get
        { url = "http://localhost:5067/exam"
        , expect = Http.expectJson FetchQuestionComplete questionDecoder
        }

questionDecoder : Decode.Decoder Question
questionDecoder =
    Decode.map2 Question
        (Decode.field "id" Decode.string)
        (Decode.field "text" Decode.string)

submitAnswer : String -> String -> Cmd Msg
submitAnswer questionId answer =
    Http.post
        { url = "http://localhost:5067/answer"
        , body = Http.jsonBody (answerEncoder questionId answer)
        , expect = Http.expectWhatever SubmitAnswerComplete
        }

answerEncoder : String -> String -> Json.Encode.Value
answerEncoder questionId answer =
    Json.Encode.object
        [ ( "id", Json.Encode.string questionId )
        , ( "answer", Json.Encode.string answer )
        ]

-- View

httpErrorToString : Http.Error -> String
httpErrorToString error =
    case error of
        Http.BadUrl url ->
            "無効なURL: " ++ url
        Http.Timeout ->
            "タイムアウト"
        Http.NetworkError ->
            "ネットワークエラー"
        Http.BadStatus status ->
            "エラーステータス: " ++ String.fromInt status
        Http.BadBody message ->
            "エラーボディ: " ++ message

view : Model -> Element Msg
view model =
    column
        Material.column
        [ case model.questionState of
            Loading ->
                Element.text "問題を読み込み中..."

            Success question ->
                column
                    Material.column
                    [ Element.text question.text
                    --, TextField.textField
                    --    (Material.textField Material.defaultPalette)
                    --    { text = model.answerInput
                    --    , placeholder = Nothing
                    --    , onChange = AnswerInput
                    --    , label = Element.text "回答を入力"
                    --    }
                    --, Button.button
                    --    (Material.containedButton Material.defaultPalette)
                    --    { text = "解答を送信"
                    --    , icon = Element.none
                    --    , onPress = Just SubmitAnswer
                    --    }
                    ]

            Failure error ->
                Element.text ("問題の読み込みに失敗しました: " ++ httpErrorToString error)
        ]

-- Main

main : Program () Model Msg
main =
    Browser.element
        { init = \() -> ( initialModel, fetchQuestion )
        , view = \model -> Element.layout [] (view model)
        , update = update
        , subscriptions = \_ -> Sub.none
        }