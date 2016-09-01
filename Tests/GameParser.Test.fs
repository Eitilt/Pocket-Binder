(* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ *)
 
module GameParser.Test

open FsCheck
open FsUnit
open NUnit.Framework

//TODO: Figure out how to write tests for `trigger`, `triggerDown`, and `walk`

// Testing against strings is slightly more specific than ideal, but that's
// the only type the reader's going to use, anyway.

[<Test>]
let ``Reader.addOption: left identity`` () =
    fun str ->
        XmlReader.addOption None str |> should equal <| Some str

    |> Prop.forAll Arb.from<string>
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Reader.addOption: right identity`` () =
    fun (NonNull str) ->
        XmlReader.addOption (Some str) "" |> should equal <| Some str

    |> Prop.forAll Arb.from<NonNull<string>>
    |> Check.QuickThrowOnFailure

[<Test>]
let ``Reader.addOption: associativity`` () =
    fun (s1, s2) ->
        XmlReader.addOption (Some s1) s2 |> should equal <| Some (s1 + s2)

    |> Prop.forAll (Gen.two Arb.generate<string> |> Arb.fromGen)
    |> Check.QuickThrowOnFailure