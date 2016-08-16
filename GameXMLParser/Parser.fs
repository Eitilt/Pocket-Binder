(*** hide ***)
namespace GameXmlParser

(** The core feature of the app (beyond its obvious organizational tools) was
always meant to be extensibility into just about any TCG someone cared to make
a definition for. While I've become less inclined toward providing a means for
end users to make their own willy-nilly as I've developed this and its earlier
versions -- and discovered how much work necessarily goes into it -- I still
do want to make it easy (enough) for *me* to add new games. As such, I needed
some way to isolate just the data and behaviour that changes from game to game
so I wasn't left editing raw code with every new "content pack".

Personally, coming from C++ and having used vim as my IDE, I would have been
happy enough with templates, generics, and a lot of inheritance, but my
Android Development professor touted the benefits of XML strongly enough that
I was convinced to switch over. Now that I've worked with it, I do definitely
agree that it makes the definitions a lot easier to write (so thanks, Brian!).

Of course, writing them in XML does require implementing a parser to turn the
definition files into something that Android recognizes. That's where this
class comes in. Before getting to the code, though, I've made two design
decisions that should probably be explained:
+   **The language:** I'm writing this in F# rather than C# not only because
    I recently completed a programming languages class where we looked at a
    lot of functional languages, but because XML's nested, almost fractal
    nature lends itself well to recursion (or at the very least, loops).
    Rather than trying to track location and state in an imperative language,
    it felt like that would match well with this.
+   **The framework:** You'll notice that I'm not using anything like
    `FSharp.Data.XML`. In fact:
**)
open System.Xml
(**
    Those libraries appear to implement more of a DOM model than any of the
    streaming ones, or their documentation is ambiguous enough that I'm not
    sure that they don't. While reading these particular files might work well
    enough, I do want to provide support for loading local (non-web-service)
    card databases in XML format, and while the definition files would very
    likely be all right, I'm wary about trying to get an entire card database
    into a phone's RAM at once. Rather than writing interfaces for two
    separate frameworks, it made sense to just use the built-in `XMLReader`,
    which is explicitly advertised as providing "fast, noncached, forward-only
    access to XML data."

    I don't need any more than that.
**)


(**
Reading
=======
**)
module Reader =

(** Before we can parse anything, we need to be able to read through the file.
Due to that same nested nature that led to me choosing F#, that's slightly
more involved than simply reading line-by-line; we need to keep track of where
the "cursor" is in order to parse the structure correctly.

Position
--------
We can treat that location as a single, non-branching path as long as we save
the data in some other way once we hit a closing tag, and since we don't want
to be operating on raw XML tags for the rest of the program anyway, that's not
an unreasonable requirement. Therefore, for this section, we only need a way
to represent tags (including any potential attributes they might have).
**)
    type Attribute = string * string
    type Tag = {
        Elem : string
        Atts : Attribute list
(** I am counting contained text as an intrinsic part of the tag as, if we do
anything to a tag, it is as likely to involve that as any of the attributes.
**)
        Text : string option
    }

(** F# records are definitely handy but their initial creation can be a bit
verbose, so it helps to create an empty prototype and use the `with` syntax.
**)
    let newTag = {
        Elem = ""
        Atts = []
        Text = None
    }

(** And for the final bit of setup, create helper functions to simplify adding
data to the non-trivial fields in `Tag`.
**)
    let rec readAttrs (reader : XmlReader) tag =
        if reader.MoveToNextAttribute ()
            then { tag with
                     Atts = (reader.LocalName, reader.ReadContentAsString ()) :: tag.Atts
                 } |> readAttrs reader
            else tag
(** Admittedly, `readAttrs` pretty much has to be a recursive function unless
we want to create an explicit loop, but one of my favorite things about
functional languages is how they encourage short, single-purpose functions,
and I just found this extra `match` to look too wordy if it's written out in
another function.
**)
    let inline addOption existing addition =
        match existing with
        | None   -> Some addition
        | Some e -> Some (e + addition)

(**
Node handling
-------------
The first smallest sensible segment to define is a way to represent the nodes
in the file -- this will be similar to System.Xml.XmlNodeType, but allows
passing the content alongside the type.
**)
    type Content =
        | EOF
        | StartTag of Tag
        | EndTag   of string
        | Text     of string
(** One idiosyncrasy that needs to be handled, though, is that a self-closing
tag (`<tag />`) only generates a `Element` event, without an `EndElement`. As
we will be relying on the latter to pop tags from the location stack, we need
a separate case to indicate that we shouldn't expect that to happen.
**)
        | EmptyTag of Tag

(** Obtaining `Content` values is likewise rather unremarkable, being
essentially just a simple way to translate from the `XmlNodeType`. The most
interesting part of it is in the match for `Element`, when it makes use of the
first-class functions to apply the same arguments to multiple types of `Content`
for less rewriting of code.
**)
    let rec parseTag (reader : XmlReader) =
        if reader.Read ()
            then match reader.NodeType with
                 | XmlNodeType.Element    -> (if reader.IsEmptyElement
                                                  then EmptyTag
                                                  else StartTag) (readAttrs reader { newTag with Elem = reader.LocalName })
                 | XmlNodeType.EndElement -> EndTag reader.LocalName
                 | XmlNodeType.Text       -> Text (reader.ReadContentAsString ())
(** We *will* need to handle unrecognized or unnecessary nodes eventually.
Doing so in this function makes the most sense from the data perspective since
we need to return some `Content` value; if we extract that handling to
elsewhere, we'd need to write an additional `Content.Unknown` case.
Additionally, this is where we increment the XML cursor, so it provides good
encapsulation to continue doing so until we reach something we can use. The
one thing that we lose by putting it here is the ability to customize the
handling, but that's only rarely going to be helpful, and not at all in this
particular project.
**)
                 | _                      -> parseTag reader
            else EOF

(**
Streaming
---------
Since we're using `XMLReader` rather than DOM as explained above, we need to
handle the behaviour behind the reading ourselves. Luckily, that's simply a
loop -- or a recursive function call. We already take care of the increment
step in `parseTag`, so all that's left is to handle that result and ensure we
continue reading through the entire function. That function is included here,
but it uses a few concepts described later.
**)
(*** include: function-walk ***)
(** Importantly, `walk` is able to be optimized through tail recursion: the
code is run once for every tag in the file and every other type of node
handled by `parseTag`, and could easily overflow the stack if each of those
generated a wholly new function call with its own frame.
**)

(**
Data callbacks
--------------
Until this point, we've focused purely on document-agnostic reading, but in
order to make it worthwhile, we need some way of specializing the behaviour to
reflect what we're wanting to do with that data. Even with the first-class
functions, it's neither particularly portable nor scalable to try to do so in
a purely functional manner -- access time is likely not going to be a factor
as it would depend on the number of handled tag *names*, but constructing the
`Map` (or however else it's implemented) would result in a somewhat ridiculous
number of pipes or an equally large array of functions in tuples. Instead, we
borrow an imperative data structure from the .NET backing and use that to keep
track of our callbacks.
**)
    type CallbackStore = System.Collections.Generic.Dictionary<string, (Tag -> unit)>

(** While we could operate on that directly, it is better to provide a more
encapsulated means of triggering any particular callback, in case we want to
have an internal hook in the future or anything. Besides, it makes the code
clearer to read through.
**)
    let trigger (store : CallbackStore) tag = store.Item tag.Elem tag

(** This, though, can potentially be a lot more important -- if any of the
tags aren't closed properly (whether due to using an HTML style of nesting or
because of a misspelled closing tag), this runs the proper callback for any
tags that might have been skipped once we do reach a node we recognize. Until
we do, though, we don't trigger any of the callbacks as we have no guarantee
that they aren't part of a still-incomplete enclosing tag.
**)
(*** include: function-triggerDown ***)
(** Note the returned value: because multiple tags may have been processed in
a single call to `triggerDown`, we need to provide some way for the caller to
know what tags remain. Since they're rarely going to be doing anything further
to any that have already been processed, we just return the rest of the list.
They can call `List.splitAt returned.Length cursor` if they really want it.

And this is the internal function for determining what tags to trigger. It's
not actually hugely different than `List.takeWhile` with `head.Elem <> elem`
other than including the first item for which the condition does not hold, but
doing so inlined rather than calling `List.item takeWhile.Length cursor` gets
rid of a second O(n) lookup. Likewise, but more importantly, prepending each
item to the front of the path greatly simplifies the complexity (which would
be O(n²) otherwise) at the smaller cost of requiring the list be reversed.
**)
    let rec triggerDown' elem cursor path =
        match cursor with
        | (head :: tail) -> if head.Elem = elem
                                then (head :: path)
                                else triggerDown' elem tail (head :: path)
        | []             -> []
(** Allowing tail optimizations by including `path` in the arguments likely
does not hugely affect anything, but I figure it's still good practice at the
very least, and there's no harm in helping the phones along wherever possible.
**)

(*** hide ***)
// The literate docs are before the previous function
(*** define: function-triggerDown ***)
    let triggerDown store elem cursor =
        match List.rev (triggerDown' elem cursor []) with
        | []   -> cursor
        | tags -> List.iter (trigger store) tags
                  List.skip tags.Length cursor

(*** hide ***)
// The literate docs are before the section on callbacks
(*** define: function-walk ***)
    let rec walk store reader cursor =
        match parseTag reader with
        | EOF          -> ignore <| triggerDown store (List.last cursor).Elem cursor
        | StartTag tag -> walk store reader (tag :: cursor)
        | EndTag elem  -> match cursor with
                          | [] -> walk store reader cursor
                          | _  -> triggerDown store elem cursor
                                  |> walk store reader
        | Text text    -> match cursor with
                          | (head :: tail) -> { head with Text = addOption head.Text text } :: tail
                                              |> walk store reader
                          | []             -> walk store reader cursor
        | EmptyTag tag -> trigger store tag
                          walk store reader cursor
