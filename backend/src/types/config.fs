module Config


type Box = {
    Name: string
    Paths: string list
}

type Config = {
    Boxes: Box list
}