module Test exposing (main)

import Browser
import Element exposing (Element)
import Widget exposing (column)
import Widget as Button
import Widget.Material as Material
import Widget as TextField

main : Program () () ()
main =
    Browser.element
        { init = \() -> ( (), Cmd.none )
        , view = \() ->
            Element.layout [] <|
                column  Material.column
                    [ --TextField.textField
                      --  (Material.textField Material.defaultPalette)
                      --  { text = ""
                      --  , placeholder = Nothing
                      --  , onChange = \_ -> ()
                      --  , label = Element.text "Test Input"
                      --  }
                     --Button.button
                     --   (Material.containedButton Material.defaultPalette)
                     --   { text = "Test Button"
                     --   , icon = Element.none
                     --   , onPress = Nothing
                     --   }
                    ]
        , update = \msg model -> ( model, Cmd.none )
        , subscriptions = \_ -> Sub.none
        }