(* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *)


(** XML Reader
==============
**)
module GameParser.XmlReader

(** The core feature of the app (beyond its obvious organizational tools) was
always meant to be extensibility into just about any TCG someone cared to make
a definition for. While I've become less inclined toward providing a means for
end users to make their own willy-nilly as I've developed this and its earlier
versions -- and discovered how much work winds up going into it -- I still do
want to make it easy (enough) for *me* to add new games. As such, I needed
some way to isolate just the data and behaviour that changes from game to game
so I wasn't left editing raw code with every new "content pack".

Personally, coming from C++ and having used vim as my IDE, I would have been
happy enough with templates, generics, and a lot of inheritance, but my
Android Development professor touted the benefits of XML strongly enough that
I was convinced to switch over. Now that I've worked with it, I do definitely
agree that it makes the definitions a lot easier to write (so thanks, Brian!).

Of course, writing them in XML does require implementing a parser to turn the
definition files into something that the computer can work with. That's where
this class comes in. Before getting to the code, though, I've made two design
decisions that should probably be explained:
+   **The language:** I'm writing this in F# rather than C# not only because
    I recently completed a programming languages class where we looked at a
    number of functional languages, but because XML's nested, almost fractal
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
    sure that they don't. While reading game definition files in that manner
    might work well enough, I do want to provide support for loading local
    card databases in XML format, and I'm wary about trying to get an entire
    game's worth of cards into a phone's RAM at once. Rather than writing
    interfaces for two separate frameworks, it made sense to just use the
    built-in `XMLReader`, which is explicitly advertised as providing "fast,
    noncached, forward-only access to XML data."

    I don't need any more than that.
**)


(**
Position
--------

Before we can parse anything, we need to be able to read through the file. Due
to that same nested nature that led to me choosing F#, that's slightly more
involved than simply reading line-by-line; we need to keep track of where the
"cursor" is in order to parse the structure correctly.

We can treat that location as a single, non-branching path as long as we save
the data in some other way once we hit a closing tag, and since we don't want
to be operating on raw XML tags for the rest of the program anyway, that's not
an unreasonable requirement. Therefore, for this section, we only need a way
to represent those tags (including any potential attributes they might have).
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

(** And for the final bit of setup, create a couple helper functions to
simplify adding data to the non-trivial fields in `Tag`:
**)
let rec readAttrs (reader : XmlReader) tag =
    if reader.MoveToNextAttribute ()
        then { tag with Atts = (reader.LocalName, reader.ReadContentAsString ()) :: tag.Atts }
             |> readAttrs reader
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
Additionally, this is where we increment the 'XmlReader' cursor, so it
provides good encapsulation to continue doing so until we reach something we
can use. The one thing that we lose by putting it here is the ability to
customize the handling, but that's only rarely going to be helpful, and not at
all in this particular project.
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
but it calls a few concepts described later.
**)
(*** include: function-read-header ***)
(*** include: function-read-call ***)
(** It is definitely best to implement this with a tail-recursive function as
the code is run once for every type of node handled by `parseTag`, and could
easily overflow the stack if each of those generated a wholly new function
call with its own frame. Doing so, however, requires that the function is
passed the path and the data resulting from the callbacks, and it's best to
encapsulate doing so for simplicity and error prevention.

Of the parameters the user does need to provide, `store` contains a lookup
table to transform any raw tag into a data type of the their own creation,
which allows them to hopefully apply any optimizations they can to reduce the
memory footprint, and `reader` is, unsurprisingly,
**)
(*** include: function-read-inner ***)

(** While the code for each case isn't particularly long, reading it is no
longer trivial, so it's probably best to explain them individually or in
small groups:
**)

(*** include: function-read-startend ***)
(** For basic tags, we only have to mark when they start in the list of
processed data and add the tag to the path. Most of the logic occurs when the
tag is closed, as that's when we send the user both the data it contains and
the raw tag itself, as described in the next section.
**)

(*** include: function-read-empty ***)
(** As mentioned, XmlReader doesn't generate any `EndTag`-type signal for
empty tags, so we need to handle them separately. This is a simplification of
the logic within `triggerDown` from before, as we know we're only going to be
dealing with a single tag; likewise, since we essentially "enter" and then
immediately "leave" the tag, there's no net change to `path`. We do have to
prepend a `Start` marker to the `data`, though, as `trigger` consumes that.
**)

(*** include: function-read-text ***)
(** Text is even simpler, as we just need to add it to the nearest enclosing
tag. This current implementation is probably going to format it weirdly if
other tags are included within it (e.g. HTML formatting) even beyond losing
track of where the latter were located, but fixing that's not a priority yet.
**)

(*** include: function-read-eof ***)
(** Finally, if we reach the end of the file, we want to complete whatever
processing we've still got on the queue. `triggerDown` doesn't have a simple
way to specify "do everything in this list", so we again duplicate its
internals to finish the file out. Note that this will result in a list of all
top-level tags in the XML file.
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
type OpeningCallback        = Tag -> unit
type ClosingCallback<'V>    = Tag -> 'V seq -> 'V option
type CallbackDictionary<'V> = System.Collections.Generic.Dictionary<string, (OpeningCallback option * ClosingCallback<'V> option)>

(** The signatures used for the callbacks will be discussed soon but, simply,
allow the user to both receive notification of opening tags (for example, to
obtain the top-level attributes without waiting to read the entire file) and
to process a recently-closed tag along with the data they've generated from
all of its children. Unfortunately, the latter does mean that there's a
tendency to retain the entire (processed) file in memory, which rather negates
the benefits of choosing the `XmlReader` over `XmlDocument`, but at the same
time, this method allows the user to prune out any unnecessary data from the
tags, and also to drop the tag entirely if it can, for example, be written to
an external file.

Given that we have streaming access and, as will be seen later, properly parse
unclosed tags, there's no easy way to skip an entire tag; I looked at having
the opening `Tag -> unit` callback return a `bool` instead for that purpose,
but came to the conclusion that, if they really want to, the user can maintain
their own logic for skipping tag contents -- which will only come into play if
they have tags within one to skip that they do want to parse elsewhere, and
that processing involves some sort of side effects.

Meanwhile, the closing callback `Tag -> 'V seq -> 'V option` is first passed
the triggering `Tag` followed by all the data generated by the callbacks from
that tag's descendants, and can process it all (likely into another case of
the union representing the data) before returning. This is the primary means
of skipping a tag, as returning `None` will discard all the data that the
callback was passed; if you just want to leave a tag and its children
untouched, don't specify a closing callback.

As that dictionary has a rather complex signature, we wrap it in another type:
**)
type CallbackStore<'V> (m) = 
    let map : CallbackDictionary<'V> = m

(*** hide ***)
// The literate docs are at the end of the type definition
(*** define function-cstore-item ***)
    let item elem =
        try
            map.Item elem
        with
        | :? System.Collections.Generic.KeyNotFoundException -> None, None

(** We also provide a pair of usability constructs for initializing an empty
store and for copying an existing store.
**)
    new () = CallbackStore<'V> (new CallbackDictionary<'V> ())
    member this.callbacks = map

(** With the values being encapsulated in a tuple and callbacks for the start
tags likely much less common than those for the end, it would definitely be
best to allow modification of one while abstracting away the other.
**)
    member this.setOpen elem callback =
        if map.ContainsKey elem
            then let _, close = map.Item elem
                 map.Add (elem, (Some callback, close))
            else map.Add (elem, (Some callback, None))

    member this.setClose elem callback =
        if map.ContainsKey elem
            then let opening, _ = map.Item elem
                 map.Add (elem, (opening, Some callback))
            else map.Add (elem, (None, Some callback))

(** These getters are rather straightforward, just abstracting away the way
the callbacks are internally stored.
**)
    member this.getOpen elem =
        let first, _ = item elem
        first

    member this.getClose elem =
        let _, second = item elem
        second
(** They do, however, involve a bit of indirection to support elements that
have not actually been associated with any callbacks -- doing so internally
makes the external code much simpler.
**)
(*** include: function-cstore-item ***)

(** And, finally, provide a function that allows simple construction of a
`CallbackStore` in a more functional style:
**)
type Callbacks<'V> =
    | Opening of OpeningCallback
    | Closing of ClosingCallback<'V>
    | Both    of OpeningCallback * ClosingCallback<'V>

let generateStore callbacks =
    let rec generate (store : CallbackStore<_>) cbs =
        match cbs with
        | []             -> store
        | (head :: tail) ->
            match head with
            | elem, Opening o   -> store.setOpen  elem o
                                   generate store tail
            | elem, Closing c   -> store.setClose elem c
                                   generate store tail
            | elem, Both (o, c) -> store.setOpen  elem o
                                   store.setClose elem c
                                   generate store tail

    generate (new CallbackStore<_> ()) callbacks

(** To better encapsulate the callbacks, we wrap the access and application in
another pair of functions; it's a good idea to begin with, and it's even nicer
now that this module is managing the data the callbacks generate itself.
**)
let triggerOpen (store : CallbackStore<_>) tag =
    match store.getOpen tag.Elem with
    | Some callback -> callback tag
    | None          -> ()

(** And then the closing callbacks; much more complex because we need to deal
with the list of data. First we make things cleaner by defining a couple
aliases that provide better readability of how the `Option` cases are used.
**)
let startTag = None
let isStartTag = Option.isNone

(** Even so, this part of the code is admittedly a bit messy, but it's not
actually doing all that much:
**)
let triggerClose (store : CallbackStore<_>) data tag =
(** 1.  Find the index of the most recent start tag indicator, defaulting to a
        value that will leave the entire list in the first segment.
**)
    let splitIndex =
        Seq.tryFindIndex isStartTag data
        |> function Some i -> i
                  | None   -> Seq.length data
(** 2.  Split the list at that index so the data from tags within the current
        one (`this`) can be manipulated separately from that generated by the
        siblings or parent's siblings or so on of the current tag (`older`),
        and discard the separator between the two groups.
**)
    let (this, older) = match List.splitAt splitIndex data with
                        | (head, [])        -> (head, [])
                        | (head, _ :: tail) -> (head, tail)
(** 3.  Try to obtain the callback, properly handling its (non-)existence.
**)
    match store.getClose tag.Elem with
(** 4.  If there is one, retrieve the data from the `Option` wrapper used to
        indicate start tags by running the list through `Seq.choose`; we know
        that `this` doesn't contain any separator elements (and so could use a
        `Seq.map`), but we have to make the type-checker happy.
**)
    | Some callback -> match (Seq.choose id this
(** 5.  Send the callback the naked data...
**)
                              |> callback tag) with
(** 6.  ...and add what it returns to the head of the list *as long as* it did
        generate something; using `Option` to indicate `startTag`s makes the
        positive case simpler, but would confuse it if we added `None`s into
        the list (they're what indicates a tag break).
**)
                       | Some _ as tagData -> tagData :: older
                       | None              -> older
(** 7.  If, however, there's no callback associated with the tag, simply join
        the lists back together. Note that the resulting list is not exactly
        the same as what came in (unless `older = []`) as the intermediate
        `startTag` has been removed; this is desired as otherwise we would be
        left with, essentially, an unclosed tag, and future calls to this
        function would split the data list at improper locations.
**)
    | None          -> List.concat [this ; older]

(** This next function may be unnecessary in ideal situations, but it does
serve as a good safety for real-world use -- if any of the tags aren't closed
properly (whether due to using an HTML style of nesting or because the closing
tag was misspelled), this runs the proper callback for any tags that weren't
previously processed once we do reach a recognized node. Until we do, though,
we don't trigger any of the callbacks as we have no guarantee that they aren't
part of a still-incomplete enclosing tag (spellcheck is beyond the scope of
the parser). Be warned that it will still fail if the malformed tag is nested
another with the same (but correctly-closed) name.
**)
let triggerDown store elem (data, path) =
(*** include: function-triggerDown-body ***)
(** Note the returned value: because multiple tags may have been processed in
a single call to `triggerDown`, we need to provide some way for the caller to
know what tags remain. Since they're rarely going to be doing anything further
to any that have already been processed, we just return the rest of the list.
They can call `List.splitAt returned.Length path` if they really want it.

And this is the internal function for determining what tags to trigger. It's
not actually hugely different than `List.takeWhile` with `head.Elem <> elem`
other than including the first item for which the condition does not hold, but
doing so inlined with the main selection rather than calling `List.item
takeWhile.Length path` afterward gets rid of a second O(n) lookup. Likewise,
but more importantly, prepending each item to the front of the list instead of
the end greatly simplifies the complexity (which would be O(n²) otherwise) at
the smaller cost of requiring the list be reversed afterward.
**)
    let rec triggerDown' elem toCheck foundPath =
        match toCheck with
        | []             -> []
        | (head :: tail) ->
            if head.Elem = elem
                then (head :: foundPath)
                else triggerDown' elem tail (head :: foundPath)
(** Allowing tail optimizations by passing `foundPath` likely does not hugely
affect anything, but I figure it's still good practice at the very least, and
there's no harm in helping the phones along wherever possible.
**)

(*** hide ***)
// The literate docs are at the beginning of the function
(*** define: function-triggerDown-body ***)
    match triggerDown' elem path [] |> List.rev with
    | []   -> data, path
    | tags -> (List.fold (triggerClose store) data tags), (List.skip tags.Length path)

(*** hide ***)
// The literate docs are before the section on callbacks
(*** define: function-read-header ***)
let read store reader =
(*** define: function-read-inner ***)
    let rec read' store' reader' ((data, path) as state) =
        match parseTag reader' with
(*** define: function-read-eof ***)
        | EOF          -> let final = List.fold (triggerClose store') data path
                          Seq.choose id final
(*** define: function-read-startend ***)
        | StartTag tag -> triggerOpen store' tag
                          read' store' reader' (startTag :: data, tag :: path)
        | EndTag elem  -> match path with
                          | []             -> read' store' reader' state
                          | _              -> triggerDown store' elem state
                                              |> read' store' reader'
(*** define: function-read-text ***)
        | Text text    -> match path with
                          | []             -> read' store' reader' state
                          | (head :: tail) -> (data, { head with Text = addOption head.Text text } :: tail)
                                              |> read' store' reader'
(*** define: function-read-empty ***)
        | EmptyTag tag -> triggerOpen store' tag
                          (triggerClose store' (startTag :: data) tag, path)
                          |> read' store' reader'

(*** define: function-read-call ***)
    read' store reader ([], [])
