module Main exposing (main)

import Browser
import Element exposing (Element)
import Http
import Json.Decode as Decode
import Url
import Widget
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
    let

        answerButtons = Widget.row
            Material.row
            [ Widget.textButton
                (Material.containedButton Material.defaultPalette)
                { text = "Test Button"
                , onPress = Just SubmitAnswer
                }
            , Widget.textButton
                (Material.containedButton Material.defaultPalette)
                { text = "Test Button"
                , onPress = Just SubmitAnswer
                }
            ]
        answerButtons2 =
            Element.row
                [ Element.width Element.fill, Element.spacingXY 10 0 ]
                [ Widget.textButton
                    (Material.containedButton Material.defaultPalette)
                    { text = "Test Button"
                    , onPress = Just SubmitAnswer
                    }
                , Widget.textButton
                    (Material.containedButton Material.defaultPalette)
                    { text = "Test Button"
                    , onPress = Just SubmitAnswer
                    }
                ]
    in
        Widget.column
            Material.column
            [ case model.questionState of
                Loading ->
                    Element.text "問題を読み込み中..."

                Success question ->
                    Widget.column
                        Material.column
                        [ Element.text question.text
                        -- ,   Widget.textInput
                        --     (Material.textInput Material.defaultPalette)
                        --     { text = model.answerInput
                        --     , placeholder = Nothing
                        --     , onChange = AnswerInput
                        --     , label = "Test Input"
                        --     , chips = []
                        --     }
                        , answerButtons2
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