(* Any copyright is dedicated to the Public Domain.
   http://creativecommons.org/publicdomain/zero/1.0/
*)
 
module Database.Test

open FsCheck.NUnit

type LocalDatabaseHelper =
    [<Property(MaxTest = 10)>]
    static member ``MemoryOrLocalPath doesn't change ":memory:"`` () =
        Database.LocalDatabaseHelper.MemoryOrLocalPath(":memory:") = ":memory:"
    
    [<Property>]
    static member ``MemoryOrLocalPath doesn't add path to "file::memory:" prefix`` (str:string) =
        Database.LocalDatabaseHelper.MemoryOrLocalPath("file::memory:" + str) = "file::memory:" + str
    
    [<Property>]
    //TODO: This should probably be more restrictive, rather than matching anywhere in the string
    //      See <https://www.sqlite.org/inmemorydb.html>
    static member ``MemoryOrLocalPath doesn't change names specifying in-memory mode`` (prefix:string, suffix:string) =
        Database.LocalDatabaseHelper.MemoryOrLocalPath(prefix + "mode=memory" + suffix) = prefix + "mode=memory" + suffix
    
    //TODO: Ensure other database paths are appropriate for the platform