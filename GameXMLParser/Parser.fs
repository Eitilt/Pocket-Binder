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

type Tag(e : string, i : string option, n : string option) = 
    member this.elem = e
    member this.id = i
    member this.name = n
