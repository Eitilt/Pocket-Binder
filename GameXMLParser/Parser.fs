(**
GameXMLParser
=============
**)
namespace GameXMLParser

(** The core feature of the app (beyond its obvious organizational tools) was
always meant to be extensibility into just about any TCG someone cared to make
a definition for. While I've become less inclined toward providing a means for
end users to make their own willy-nilly as I've developed this and its earlier
versions -- and discovered how much work necessarily goes into it -- I still
do want to make it easy (enough) for *me* to add new games. As such, I needed
some way to isolate just the data and behaviour that changes from game to game
so I wasn't left editing raw code with every new "content pack".

Personally, coming from C++ and using vim as my IDE, I would have been happy
enough with templates, generics, and a lot of inheritance, but my Android
Development professor touted the benefits of XML strongly enough that I was
convinced to switch over. Now that I've worked with it, I do definitely agree
that it makes the definitions a lot easier to write (so thanks, Brian!).

Of course, writing them in XML does require implementing a parser to turn the
definition files into something that Android recognizes. That's where this
class comes in. Before getting to the code, though, I've made two design
decisions that should probably be explained:
  - **The language:** I'm writing this in F# rather than C# not only because
    I recently completed a programming languages class where we looked at a
    lot of functional languages, but because XML's nested, almost fractal
    nature lends itself well to recursion (or at the very least, loops), and
    rather than trying to track location and state through operating an
    imperative language, it felt like that would match well with this.
  - **The framework:** You'll notice that I'm not using anything like
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
-------
**)
module Reader =

(** Before we can parse anything, we need to be able to read through the file.
Due to that same nested nature that led to me choosing F#, that's slightly
more involved than simply reading line-by-line; we need to keep track of where
the "cursor" is in order to parse the structure correctly.

We can treat that location as a single, non-branching path as long as we save
the data in some other way once we hit a closing tag, and since we don't want
to be operating on raw XML tags for the rest of the program anyway, that's not
an unreasonable requirement. Therefore, for this section, we only need a way
to represent tags (including any potential attributes they might have). I am
counting contained text as an intrinsic part of the tag as, if we do anything
with the tag, it is as likely to involve that as any of the attributes.
**)
    type Attribute = string * string
    type Tag = {
        Elem : string
        Atts : Attribute list
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
data to the non-trivial fields in `Tag`. Admittedly, `readAttrs` pretty much
has to be a recursive function unless we want to create an explicit loop, but
one of my favorite things about functional languages is how they encourage
short, single-purpose functions, and I just found the extra nested `match` in
`addOption` to look too wordy if it's written out in another function.
**)
    let rec readAttrs tag (reader : XmlReader) =
        match (reader.MoveToNextAttribute ()) with
        | true  -> readAttrs { tag with Atts = (reader.LocalName, reader.ReadContentAsString()) :: tag.Atts } reader
        | false -> tag

    let inline addOption existing addition =
        match existing with
        | None   -> Some addition
        | Some e -> Some (e + addition)

(** The first smallest sensible segment to define is a way to represent the
current node in the file -- this will be similar to System.Xml.XmlNodeType,
but allows passing the content alongside the type. One idiosyncrasy that needs
to be handled, though, is that a self-closing tag (`<tag />`) only generates a
`Element` event, without an `EndElement`. As we will be relying on the latter
to pop tags from the location stack, we need a separate case to indicate them.
**)
    type Content =
        | EOF
        | StartTag of Tag
        | EndTag   of string
        | EmptyTag of Tag
        | Text     of string

(** This is likewise rather unremarkable, being essentially just a simple way
to translate from the `XmlNodeType`. The most interesting part of it is in the
match for `Element`, when it makes use of the first-class functions to apply
the same arguments to multiple types of `Content`.
**)
    let rec parseTag (reader : XmlReader) =
        if (reader.Read ())
            then match reader.NodeType with
                 | XmlNodeType.Element    -> (if reader.IsEmptyElement
                                                  then EmptyTag
                                                  else StartTag) (readAttrs { newTag with Elem = reader.LocalName } reader)
                 | XmlNodeType.EndElement -> EndTag reader.LocalName
                 | XmlNodeType.Text       -> Text (reader.ReadContentAsString ())
                 | _                      -> parseTag reader
            else EOF

    let rec walk cursor (reader : XmlReader) =
        match parseTag reader with
        | EOF          -> cursor
        | StartTag tag -> walk (tag :: cursor) reader
        | EndTag _     -> List.tail cursor
        | Text text    -> match cursor with
                          | [] -> walk cursor reader
                          | (head :: tail) -> walk ({ head with Text = addOption head.Text text } :: tail) reader
