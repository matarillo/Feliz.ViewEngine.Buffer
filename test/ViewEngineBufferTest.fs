module Tests.ViewEngineBuffer

open Feliz.ViewEngine
open Swensen.Unquote
open Xunit

type Writer = System.Buffers.ArrayBufferWriter<byte>
let inline arrayBufferWriter () = Writer ()
let inline getString (writer: Writer) = System.Text.Encoding.UTF8.GetString writer.WrittenSpan

[<Fact>]
let ``Simple text element is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.text "test"
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "test"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``Simple text element is escaped Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.text "te<st"
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "te&lt;st"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p "test"
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>test</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text is escaped Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p "te>st"
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>te&gt;st</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text property is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            prop.text "test"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>test</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with onchange handler is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            prop.onChange (fun (_: Event) -> ())
            prop.text "test"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>test</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text property is escaped Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            prop.text "tes&t"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>tes&amp;t</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text element is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            Html.text "test"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>test</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text element is escaped Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            Html.text "t\"est"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<p>t&quot;est</p>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``Closed element Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.div [
            Html.br []
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = "<div><br></div>"
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text element and class property is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            prop.className "main"
            prop.children [
                Html.text "test"
            ]
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = """<p class="main">test</p>"""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``p element with text element and classes property is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.p [
            prop.classes ["c1"; "c2"]
            prop.children [
                Html.text "test"
            ]
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = """<p class="c1 c2">test</p>"""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``h1 element with text and style property is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.h1 [
            prop.style [ style.fontSize(100); style.color("#137373") ]
            prop.text "examples"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = """<h1 style="font-size:100px;color:#137373">examples</h1>"""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``The order of properties for an element is preserved``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.link [
            prop.rel.stylesheet
            prop.type' "text/css"
            prop.href "main.css"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = """<link rel="stylesheet" type="text/css" href="main.css">"""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``h1 element with text and style property with css unit is Ok``() =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.h1 [
            prop.style [ style.fontSize(length.em(100)) ]
            prop.text "examples"
        ]
        |> render.htmlView
    let result = getString writer

    // Assert
    let expected = """<h1 style="font-size:100em">examples</h1>"""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``Void tag in XML should be self closing tag`` () =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.br [] |> render.xmlView
    let unary = getString writer

    // Assert
    let expected = "<br />"
    test <@ bytes = expected.Length @>
    test <@ unary = expected @>

[<Fact>]
let ``Void tag in HTML should be unary tag`` () =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.br [] |> render.htmlView
    let unary = getString writer

    // Assert
    let expected = "<br>"
    test <@ bytes = expected.Length @>
    test <@ unary = expected @>

[<Fact>]
let ``None tag in HTML should render nothing`` () =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer

    // Act
    let bytes =
        Html.none |> render.htmlView
    let result = getString writer

    // Assert
    let expected = ""
    test <@ bytes = expected.Length @>
    test <@ result = expected @>

[<Fact>]
let ``Nested content should render correctly`` () =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer
    let nested =
        Html.div [
            Html.comment "this is a test"
            Html.h1 [ Html.text "Header" ]
            Html.p [
                Html.rawText "<br/>"
                Html.strong [ Html.text "Ipsum" ]
                Html.text " dollar"
            ]
        ]

    // Act
    let bytes =
        nested
        |> render.xmlView
    let html = getString writer

    // Assert
    let expected = "<div><!-- this is a test --><h1>Header</h1><p><br/><strong>Ipsum</strong> dollar</p></div>"
    test <@ bytes = expected.Length @>
    test <@ html = expected @>

[<Fact>]
let ``Fragment works correctly`` () =
    // Arrange
    let writer = arrayBufferWriter ()
    let render = Renderer writer
    let withFragment =
        Html.div [
            prop.className "test-class"
            prop.children [
                Html.p "test outer p"
                React.fragment [
                    Html.p "test inner p"
                    Html.span "test span"
                ]
            ]
        ]
    
    // Act
    let bytes =
        withFragment
        |> render.htmlView
    let html = getString writer

    // Assert
    let expected = """<div class="test-class"><p>test outer p</p><p>test inner p</p><span>test span</span></div>"""
    test <@ bytes = expected.Length @>
    test <@ html = expected @>
