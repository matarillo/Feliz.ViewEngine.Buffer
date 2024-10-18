// ---------------------------
// Attribution to original authors of this code
// ---------------------------
// This code has been originally ported from Giraffe which was originally ported from Suave with small modifications
// afterwards.
//
// The original code was authored by
// * Dustin Moris Gorski (https://github.com/dustinmoris)
// * Henrik Feldt (https://github.com/haf)
// * Ademar Gonzalez (https://github.com/ademar)
//
// You can find the original implementations here:
// - https://github.com/giraffe-fsharp/Giraffe/blob/master/src/Giraffe/GiraffeViewEngine.fs
// - https://github.com/SuaveIO/suave/blob/master/src/Suave.Experimental/ViewEngine.fs
//

namespace Feliz.ViewEngine

open System
open System.Text
open System.Buffers

type IReactProperty =
    | KeyValue of string * obj
    | Children of ReactElement list
    | Text of string


and ReactElement =
    | Element of string * IReactProperty list // An element which may contain properties
    | VoidElement of string * IReactProperty list // An empty self-closed element which may contain properties
    | TextElement of string
    | Elements of ReactElement seq

[<RequireQualifiedAccess>]
module ViewBuilder =
    let getEscapeSequence c =
        match c with
        | '<'  -> "&lt;"
        | '>'  -> "&gt;"
        | '\"' -> "&quot;"
        | '\'' -> "&apos;"
        | '&'  -> "&amp;"
        | ch -> ch.ToString()

    let escape str = String.collect getEscapeSequence str

    let inline private (+=) (sb : StringBuilder) (text : string) = sb.Append(text)
    let inline private (+!) (sb : StringBuilder) (text : string) = sb.Append(text) |> ignore

    let inline private selfClosingBracket (isHtml : bool) =
        if isHtml then ">" else " />"

    let rec private buildNode (isHtml : bool) (sb : StringBuilder) (node : ReactElement) : unit =
        let splitProps (props: IReactProperty list) =
            let init = [], None, []
            let folder (prop: IReactProperty) ((children, text, attrs) : ReactElement list * string option * (string*obj) list) =
                match prop with
                | KeyValue (k, v) -> children, text,  (k, v) :: attrs
                | Children ch -> List.append children ch, text, attrs
                | Text text -> children, Some text, attrs
            List.foldBack folder props init

        let buildElement closingBracket (elemName, props : (string*obj) list) =
            match props with
            | [] -> do sb += "<" += elemName +! closingBracket
            | _    ->
                do sb += "<" +! elemName

                props
                |> List.iter (fun (key, value) ->
                    sb += " " += key += "=\"" += value.ToString () +! "\"")

                sb +! closingBracket

        let buildParentNode (elemName, attributes : (string*obj) list, nodes : ReactElement list) =
            buildElement ">" (elemName, attributes)
            for node in nodes do
                buildNode isHtml sb node
            sb += "</" += elemName +! ">"

        match node with
        | TextElement text -> sb +! text
        | VoidElement (name, props) ->
            let _, _, attrs = splitProps props
            buildElement (selfClosingBracket isHtml) (name, attrs)
        | Element (name, props) ->
            let children, text, attrs = splitProps props
            match children, text, attrs with
            | _, Some text, _ -> buildParentNode (name, attrs, TextElement text :: children)
            | _ -> buildParentNode (name, attrs, children)
        | Elements elements ->
            for element in elements do
                buildNode isHtml sb element

    let buildXmlNode  = buildNode false
    let buildHtmlNode = buildNode true

    let buildXmlNodes  sb (nodes : ReactElement list) = for n in nodes do buildXmlNode sb n
    let buildHtmlNodes sb (nodes : ReactElement list) = for n in nodes do buildHtmlNode sb n

    let buildHtmlDocument sb (document : ReactElement) =
        sb += "<!DOCTYPE html>" +! Environment.NewLine
        buildHtmlNode sb document

    let buildXmlDocument sb (document : ReactElement) =
        sb += """<?xml version="1.0" encoding="utf-8"?>""" +! Environment.NewLine
        buildXmlNode sb document

// fsharplint:disable

/// Render HTML/XML views fsharplint:disable
type Render =
    /// Create XML view
    static member xmlView (node: ReactElement) : string =
        let sb = StringBuilder() in ViewBuilder.buildXmlNode sb node
        sb.ToString()

    /// <summary>Create XML view</summary>
    static member xmlView (nodes: ReactElement list) : string =
        let sb = StringBuilder() in ViewBuilder.buildXmlNodes sb nodes
        sb.ToString()

    /// Create XML document view with <?xml version="1.0" encoding="utf-8"?>
    static member xmlDocument (document: ReactElement) : string =
        let sb = StringBuilder() in ViewBuilder.buildXmlDocument sb document
        sb.ToString()

    /// Create HTML view
    static member htmlView (node: ReactElement) : string =
        let sb = StringBuilder() in ViewBuilder.buildHtmlNode sb node
        sb.ToString()

    /// Create HTML view
    static member htmlView (nodes: ReactElement list) : string =
        let sb = StringBuilder() in ViewBuilder.buildHtmlNodes sb nodes
        sb.ToString()

    /// Create HTML document view with <!DOCTYPE html>
    static member htmlDocument (document: ReactElement) : string =
        let sb = StringBuilder() in ViewBuilder.buildHtmlDocument sb document
        sb.ToString()


[<RequireQualifiedAccess>]
module ViewWriter =
    type BufferWriter = IBufferWriter<byte>

    type State = ValueTuple<BufferWriter, int64>

    let utf8 = Encoding.UTF8
    
    let getEscapeSequence c =
        match c with
        | '<'  -> "&lt;"
        | '>'  -> "&gt;"
        | '\"' -> "&quot;"
        | '\'' -> "&apos;"
        | '&'  -> "&amp;"
        | ch -> ch.ToString()

    let escape str = String.collect getEscapeSequence str

    let inline private (+=) struct (writer: BufferWriter, bytes: int64) (text : string) =
        let written = utf8.GetBytes(text, writer)
        struct (writer, bytes + written)

    let inline private selfClosingBracket (isHtml : bool) =
        if isHtml then ">" else " />"

    let rec private writeNode (isHtml : bool) (state: State) (node : ReactElement) : State =
        let splitProps (props: IReactProperty list) =
            let init = [], None, []
            let folder (prop: IReactProperty) ((children, text, attrs) : ReactElement list * string option * (string*obj) list) =
                match prop with
                | KeyValue (k, v) -> children, text,  (k, v) :: attrs
                | Children ch -> List.append children ch, text, attrs
                | Text text -> children, Some text, attrs
            List.foldBack folder props init

        let writeElement (state: State) closingBracket (elemName, props : (string*obj) list) =
            match props with
            | [] ->
                state += "<" += elemName += closingBracket
            | _    ->
                let state = state += "<" += elemName
                let state = List.fold (fun s (key, value) -> s += " " += key += "=\"" += value.ToString () += "\"") state props
                state += closingBracket

        let writeParentNode (state: State) (elemName, attributes : (string*obj) list, nodes : ReactElement list) =
            let state = writeElement state ">" (elemName, attributes)
            let state = Seq.fold (writeNode isHtml) state nodes
            state += "</" += elemName += ">"

        match node with
        | TextElement text -> state += text
        | VoidElement (name, props) ->
            let _, _, attrs = splitProps props
            writeElement state (selfClosingBracket isHtml) (name, attrs)
        | Element (name, props) ->
            let children, text, attrs = splitProps props
            match children, text, attrs with
            | _, Some text, _ -> writeParentNode state (name, attrs, TextElement text :: children)
            | _ -> writeParentNode state (name, attrs, children)
        | Elements elements ->
            Seq.fold (writeNode isHtml) state elements

    let writeXmlNode state = writeNode false state
    let writeHtmlNode state = writeNode true state

    let writeXmlNodes (state: State) (nodes : ReactElement list) = List.fold writeXmlNode state nodes
    let writeHtmlNodes (state: State) (nodes : ReactElement list) = List.fold writeHtmlNode state nodes

    let writeHtmlDocument (state: State) (document : ReactElement) =
        let state = state += "<!DOCTYPE html>" += Environment.NewLine
        writeHtmlNode state document

    let writeXmlDocument (state: State) (document : ReactElement) =
        let state = state += """<?xml version="1.0" encoding="utf-8"?>""" += Environment.NewLine
        writeXmlNode state document

type Renderer (writer: IBufferWriter<byte>) =
    /// <summary>Write XML view</summary>
    member _.xmlView (node: ReactElement) : int64 =
        let struct (_, bytes) = ViewWriter.writeXmlNode struct (writer, 0L) node
        bytes

    /// <summary>Write XML view</summary>
    member _.xmlView (nodes: ReactElement list) : int64 =
        let struct (_, bytes) = ViewWriter.writeXmlNodes struct (writer, 0L) nodes
        bytes

    /// <summary>Write XML document view with &lt;?xml version="1.0" encoding="utf-8"?&gt;</summary>
    member _.xmlDocument (document: ReactElement) : int64 =
        let struct (_, bytes) = ViewWriter.writeXmlDocument struct (writer, 0L) document
        bytes

    /// <summary>Write HTML view</summary>
    member _.htmlView (node: ReactElement) : int64 =
        let struct (_, bytes) = ViewWriter.writeHtmlNode struct (writer, 0L) node
        bytes

    /// <summary>Write HTML view</summary>
    member _.htmlView (nodes: ReactElement list) : int64 =
        let struct (_, bytes) = ViewWriter.writeHtmlNodes struct (writer, 0L) nodes
        bytes

    /// <summary>Write HTML document view with &lt;!DOCTYPE html&gt;</summary>
    member _.htmlDocument (document: ReactElement) : int64 =
        let struct (_, bytes) = ViewWriter.writeHtmlDocument struct (writer, 0L) document
        bytes
