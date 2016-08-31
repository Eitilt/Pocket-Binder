Pocket Binder backend
=====================

Description
-----------
This is the backend (logic and data) code for the Pocket Binder app, which in
turn provides a way for collectors and players of trading card games to record
and manage their collections and decks, without needing to transcribe the data
for each card themselves. The code constructing the user interface is not and
will not be included here unless the project as a whole is abandoned without
being handed over to another primary developer; retaining control over that
portion allows, among other things, the option of commercially distributing
the app when otherwise I would essentially be only able to charge for having
precompiled it -- I do see a lot of benefit in open source code, and would
love to see people finding a use for my work or portions thereof.

The repository contains several closely-tied but separate packages, as
determined by the scope and behaviour of their contained code:

+ `GameParser`
  reads the files describing a particular game (the official cards, as well as
  how to store and present the data on them), whether locally or from internet
  services, and produces an internal database to allow the other packages to
  more easily obtain the elements they make use of.
+ `Tests`
  provides unit tests and property verification for the other packages.

Installation
------------
Pocket Binder was developed in Visual Studio 2015 using Xamarin for the GUI,
but it should still be more or less IDE agnostic.

As this repository only contains the backend, it is only a library without any
resulting in ultimate executable to install. Setting it up for development
(of both itself and larger programs) does, however, require a few components:

+ A .NET development environment, likely one of the following, which your
  IDE may have installed alongside itself
  - [.NET SDK](https://www.microsoft.com/net/download)
  - [Mono](http://www.mono-project.com/)
+ The [F# compiler](http://fsharp.org/) -- again, your IDE may provide this
  with an extension, which would enable better integration
+ For `GameParser`:
  - (Optional) [FSharp.Formatting](https://tpetricek.github.io/FSharp.Formatting/index.html) --
    Note that while the code is written in a literate style, FSharp.Formatting
    does not recognize the comments as they are not contained within a script
    file; this component is currently mainly for future-proofing
+ For the tests:
  - [FSCheck](https://fscheck.github.io/FsCheck/)
  - [FSUnit](http://fsprojects.github.io/FsUnit/)
  - [NUnit](http://www.nunit.org/)

Usage
-----
To be filled out later, once I have examples to reference.

License
-------
Copyright 2016 Sam May <ag.eitilt@gmail.com>

Distributed under the Mozilla Public License, v2.0 as described in the
LICENSE.txt file. Some files such as those containing unit tests are published
with no restriction placed on their use or modification; these are indicated
by a header describing their release into the public domain.