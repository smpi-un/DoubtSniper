module Test exposing (main)

import Browser
import Element exposing (Element)
import Widget exposing (..)
import Widget.Material as Material
import Widget.Icon exposing (IconStyle)
import Widget.Icon exposing (Icon)

main : Program () () ()
main =
    Browser.element
        { init = \() -> ( (), Cmd.none )
        , view = \() ->
            Element.layout [] <|
                Element.column []
                    [ Widget.textInput
                        (Material.textInput Material.defaultPalette)
                        { text = ""
                        , placeholder = Nothing
                        , onChange = \_ -> ()
                        , label = "Test Input"
                        , chips = []
                        }
                    , Widget.textButton
                        (Material.containedButton Material.defaultPalette)
                        { text = "Test Button"
                        , onPress = Nothing
                        }
                    ]
        , update = \msg model -> ( model, Cmd.none )
        , subscriptions = \_ -> Sub.none
        }
