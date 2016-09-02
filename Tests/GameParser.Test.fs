(* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/ *)
 
module GameParser.Test

open FsCheck
open FsUnit
open NUnit.Framework

open System.Xml
open System.Xml.Linq


[<Test>]
let ``Reader.addOption: left identity`` () =
    Check.VerboseThrowOnFailure <|
        fun value ->
            XmlReader.addOption None value |> should equal <| Some value

[<Test>]
let ``Reader.addOption: right identity`` () =
    Check.VerboseThrowOnFailure <|
        fun (NonNull str) ->
            XmlReader.addOption (Some str) "" |> should equal <| Some str

[<Test>]
let ``Reader.addOption: associativity`` () =
    Check.VerboseThrowOnFailure <|
        fun v1 v2 ->
            XmlReader.addOption (Some v1) v2 |> should equal <| Some (v1 + v2)


(*TODO: Complete
[<Test>]
let ``Reader.parseTag: runs trigger on entering and exiting every element`` () =
    Generators.Xml.register ()

    Check.VerboseThrowOnFailure <|
        fun (doc : XDocument) ->
            let mutable current = doc.Root
            // On entrance trigger: check that the element name is the same as `current`
            // On exit trigger: if EmptyNode, select sibling
            //                  else select parent then sibling
            // Will need to loop through manually, though, as callback database is on higher layer
            ()
*)