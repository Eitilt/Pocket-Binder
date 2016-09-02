(* Any copyright is dedicated to the Public Domain.
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * I would still, however, enjoy hearing about any improvements you make
 *)
 
module Generators

open FsCheck

open System.Text.RegularExpressions
open System.Xml.Linq

// Based on <https://gist.github.com/mavnn/5976004> and the associated post at
// <http://blog.mavnn.co.uk/fscheck-breaking-your-code-in-new-and-exciting-ways/>
module Xml =
    type ElementName = ElementName of string with
        static member op_Explicit (ElementName s) = s
    type AttributeName = AttributeName of string with
        static member op_Explicit (AttributeName s) = s

    type AttributeValue = AttributeValue of string with
        static member op_Explicit (AttributeValue s) = s
    type TextValue = TextValue of string with
        static member op_Explicit (TextValue s) = s
    type ReferenceValue = ReferenceValue of string with
        static member op_Explicit (ReferenceValue s) = s


    type Tree =
    | Leaf of string
    | Branch of string * Tree list


    let genTreeNames names =
        //TODO: Implement generation of attributes and contained text
        let rec generate size names =
            match size with
            | 0            -> Gen.map Leaf (Gen.elements names)
            | n when n > 0 ->
                Gen.oneof [
                    // Allow for jagged trees by skipping ahead an iteration
                    generate (n / 2) names

                    // Generate nested trees of decreasing size...
                    Gen.sized <| fun s ->
                        Gen.listOf (generate (n / 2) names)
                        |> Gen.resize (s |> float |> sqrt |> int)
                    // ...and encapsulate them in a randomly-named tag
                    |> Gen.map2 (fun name contents -> Branch (name, contents)) (Gen.elements names)
                ]
            | _            -> invalidArg "size" "Size most be positive."

        Gen.sized <| fun size ->
            generate size names

    let genTree =
        Gen.sized <| fun size ->
            Gen.nonEmptyListOf Arb.generate<ElementName>
            // The size calculation allows more repetition in the names while
            // not running into issues with a 0 size
            |> Gen.eval ((size / 2) + 1) (Random.newSeed ())
            |> List.map (fun (ElementName n) -> n)
            |> genTreeNames


    let genDoc tree =
        // Precompose a couple functions shared between branches
        let newElem = XName.Get >> XElement
        let newDoc  = newElem >> XDocument

        let rec generate current children =
            // Transcribe the `Tree` union into nested `XElement`s
            let fromChild child =
                match child with
                | Leaf   name            -> newElem name
                | Branch (name, subtree) -> generate (newElem name) subtree

            // Apply the above to the current node's children
            current.Add (List.map fromChild children |> List.toArray)
            current

        // Start the recursion, or use the simpler form if that's what's passed
        match tree with
        | Leaf   name            -> newDoc name
        | Branch (name, subtree) ->
            let doc = newDoc name
            generate doc.Root subtree |> ignore
            doc


    let rec shrinkTree tree =
        match tree with
        // If the node's a leaf, delete it
        | Leaf _                  -> Seq.empty
        | Branch (name, children) ->
            if children.Length <= 1
                // If it only contains a single element, turn it into a leaf node
                then Seq.singleton <| Leaf name
                // Otherwise simplify each of the node's children in turn:
                else Seq.mapi (fun i c ->
                         match shrinkTree c with
                         // If `c` was a leaf before shrinking, remove it from the list
                         // While we could match against `c` directly, this is slightly
                         //   less dependent on the exact format of the other returns
                         | e when Seq.isEmpty e -> Branch (name, Seq.toList (Seq.append (Seq.take i children) (Seq.skip (i + 1) children)))
                         // Otherwise the value returned from `shrinkTree` is already
                         // all children with `c` having been shrunk properly
                         | shrunk               -> Branch (name, Seq.toList shrunk)
                     ) children


    type Gen() =
        static let regName = "[A-Za-z_][A-Za-z0-9_\-.]*"
        static let regChar = "&#([0-9]+|x[0-9A-Fa-f]+);"
        static let regRef  = "(&" + regName + ";|" + regChar + ")"
        // Define a lookahead to prevent newlines at the end of the string
        static let regEnd  = "(?![\r\n])$"

        static let arbNonNull =
            Arb.from<string>
            // Adding an additional filter is easier than trying to rework for NonNull<string>
            |> Arb.filter (fun s -> not <| System.Object.ReferenceEquals (s, null))
        static let arbName =
            arbNonNull
            |> Arb.filter (fun s -> Regex.IsMatch (s, "^" + regName + regEnd))
            |> Arb.filter (fun s -> not <| s.ToUpper().StartsWith "XML")

        static member ElementName () =
            arbName |> Arb.convert ElementName string
        static member AttributeName () =
            arbName |> Arb.convert AttributeName string

        static member AttributeValue () =
            arbNonNull
            |> Arb.filter (fun s -> Regex.IsMatch (s, "^([^<&\"]|" + regRef + ")*" + regEnd))
            |> Arb.convert AttributeValue string
        static member TextValue () =
            arbNonNull
            // Because even entity references are counted as a separate type of node, this is simple
            |> Arb.filter (fun s -> Regex.IsMatch (s, "^[^<&]*" + regEnd))
            |> Arb.convert TextValue string
        static member ReferenceValue () =
            arbNonNull
            |> Arb.filter (fun s -> Regex.IsMatch (s, "^" + regRef + regEnd))
            |> Arb.convert ReferenceValue string


        static member Tree () = {
            new Arbitrary<Tree> () with
                override this.Generator     = genTree
                override this.Shrinker tree = shrinkTree tree
        }
        static member XDocument () = {
            new Arbitrary<XDocument> () with
                override this.Generator     = genTree |> Gen.map genDoc
                //TODO: Retrieve the `Tree` from the XDocument and shrink that
                override this.Shrinker tree = Seq.empty
        }

    //TODO: Figure out how to automatically call this (`[<StartUp>]` doesn't work with Test Explorer)
    let register =
        Arb.register<Gen> >> ignore
