# Feliz.ViewEngine.Buffer

Forked from [Feliz.ViewEngine](https://github.com/dbrattli/Feliz.ViewEngine)

## Installation

Feliz.ViewEngine.Buffer is available as a [NuGet package via GitHub Packages](https://github.com/users/matarillo/packages/nuget/package/Feliz.ViewEngine.Buffer). To install:

First,

```sh
dotnet nuget add source https://nuget.pkg.github.com/matarillo/index.json -n ANY_NAME_YOU_LIKE -u YOUR_GITHUB_ACCOUNT -p YOUR_GITHUB_PRIVATE_ACCESS_TOKEN
```

Then,

```sh
dotnet add package Feliz.ViewEngine.Buffer --version 0.27.0
```

## Getting started

```fs
open Feliz.ViewEngine

// ASP.NET Core
let writer = reponse.BodyWriter
let render = Renderer writer

let length =
    Html.h1 [
        prop.style [ style.fontSize(100); style.color("#137373") ]
        prop.text "Hello, world!"
    ]
    |> render.htmlView

// HTTP Response will be """<h1 style="font-size:100px;color:#137373">Hello, world!</h1>""" as UTF-8 bytes
```

## License

This work is dual-licensed under Apache 2.0 and MIT. You can choose between one of them if you use this work.

`SPDX-License-Identifier: Apache-2.0 OR MIT`
