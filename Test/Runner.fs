(* Any copyright is dedicated to the Public Domain.
   http://creativecommons.org/publicdomain/zero/1.0/
*)
 
open FsCheck

[<EntryPoint>]
let main argv =
    Check.QuickAll<Database.Test.LocalDatabaseHelper>()

    System.Console.WriteLine "Press any key to close"
    // Prevent the console from automatically closing
    System.Console.ReadKey true |> ignore
    // Return an integer exit code
    0
