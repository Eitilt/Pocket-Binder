(* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ *)
 
module GameParser.Test

open FsCheck
open FsUnit
open NUnit.Framework

//TODO: Figure out how to write tests for `trigger`, `triggerDown`, and `walk`

[<Test>]
let ``Reader.addOption: left identity`` () =
    Check.QuickThrowOnFailure <|
    fun value ->
        XmlReader.addOption None value |> should equal <| Some value

[<Test>]
let ``Reader.addOption: right identity`` () =
    Check.QuickThrowOnFailure <|
    fun (NonNull str) ->
        XmlReader.addOption (Some str) "" |> should equal <| Some str

[<Test>]
let ``Reader.addOption: associativity`` () =
    Check.QuickThrowOnFailure <|
    fun v1 v2 ->
        XmlReader.addOption (Some v1) v2 |> should equal <| Some (v1 + v2)
