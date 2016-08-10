module GameXMLParser.Test

// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/

open FsUnit
open FsCheck
open NUnit.Framework

[<Test>]
let ``Parser.addOption: left identity`` () =
    Prop.forAll (Arb.fromGen Arb.generate<string>) <|
        fun str -> (Reader.addOption None str) = (Some str)
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Parser.addOption: right identity`` () =
    Prop.forAll (Arb.fromGen (Gen.filter (fun (str) -> str <> null) Arb.generate<string>)) <|
        fun str -> (Reader.addOption (Some str) "") = (Some str)
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Parser.addOption: associativity`` () =
    Prop.forAll (Arb.fromGen (Gen.two Arb.generate<string>)) <|
        fun (s1, s2) -> (Reader.addOption (Some s1) s2) = (Some (s1 + s2))
    |> Check.QuickThrowOnFailure