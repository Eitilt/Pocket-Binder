module GameParser.Test

// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/

open FsUnit
open FsCheck
open NUnit.Framework

//TODO: Figure out how to write tests for `trigger`, `triggerDown`, and `walk`

[<Test>]
let ``Reader.addOption: left identity`` () =
    Prop.forAll Arb.from<string> <|
        fun str -> (XmlReader.addOption None str) = (Some str)
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Reader.addOption: right identity`` () =
    Prop.forAll (Arb.filter (fun (str) -> not <| System.Object.ReferenceEquals (str, null)) Arb.from<string>) <|
        fun str -> (XmlReader.addOption (Some str) "") = (Some str)
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Reader.addOption: associativity`` () =
    Prop.forAll (Arb.fromGen (Gen.two Arb.generate<string>)) <|
        fun (s1, s2) -> (XmlReader.addOption (Some s1) s2) = (Some (s1 + s2))
    |> Check.QuickThrowOnFailure