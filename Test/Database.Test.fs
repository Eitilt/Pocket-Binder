(* Any copyright is dedicated to the Public Domain.
   http://creativecommons.org/publicdomain/zero/1.0/
*)
 
module Database.Test

open FsCheck
open FsCheck.NUnit

type LocalDatabaseHelper =
    [<Property(MaxTest = 1)>]
    static member ``MemoryOrLocalPath doesn't change ":memory:"`` () =
        Database.DatabasePathHelper.MemoryOrLocalPath(":memory:") = ":memory:"
    
    [<Property>]
    static member ``MemoryOrLocalPath doesn't add path to "file::memory:" prefix`` (str:string) =
        Database.DatabasePathHelper.MemoryOrLocalPath("file::memory:" + str) = "file::memory:" + str
    
    [<Property>]
    //TODO: This should probably be more restrictive, rather than matching anywhere in the string
    //      See <https://www.sqlite.org/inmemorydb.html>
    static member ``MemoryOrLocalPath doesn't change names specifying in-memory mode`` (prefix:string, suffix:string) =
        Database.DatabasePathHelper.MemoryOrLocalPath(prefix + "mode=memory" + suffix) = prefix + "mode=memory" + suffix
    
    [<Property(MaxTest = 1)>]
    static member ``MemoryOrLocalPath properly handles a null argument`` () =
        //BUG: Despite the `lazy`, Prop.throws is too late to handle the exception
        //lazy Database.DatabasePathHelper.MemoryOrLocalPath(null)
        //|> Prop.throws<System.ArgumentNullException, _>
        false

    [<Property(MaxTest = 1)>]
    static member ``MemoryOrLocalPath properly handles an empty string`` () =
        //BUG: Despite the `lazy`, Prop.throws is too late to handle the exception
        //lazy Database.DatabasePathHelper.MemoryOrLocalPath("")
        //|> Prop.throws<System.ArgumentOutOfRangeException, _>
        false
    
    [<Property>]
    static member ``MemoryOrLocalPath rejects names containing invalid characters`` (name:NonNull<string>) =
        (*( name.Get.Length > 0
        && not (
            name.Get.Equals(":memory:")
            || name.Get.StartsWith("file::memory:")
            //TODO: This should probably be more restrictive, rather than matching anywhere in the string
            || name.Get.Contains("mode=memory")
        ) && ((fun c -> System.IO.Path.GetInvalidFileNameChars () |> Array.contains c)
                |> Seq.exists <| name.Get.ToCharArray ())
        //BUG: Still attempts test against empty string
        ) ==> lazy Database.DatabasePathHelper.MemoryOrLocalPath(name.Get)
        *)false

    //TODO: Do we actually want to check the platform-dependent code here?
    [<Property>]
    static member ``MemoryOrLocalPath appends the proper extension to a file name`` (name:NonNull<string>) =
       (*not (
           name.Get.Equals(":memory:")
           || name.Get.StartsWith("file::memory:")
           //TODO: This should probably be more restrictive, rather than matching anywhere in the string
           || name.Get.Contains("mode=memory")
           || (fun c -> System.IO.Path.GetInvalidFileNameChars () |> Array.contains c)
               |> Seq.exists <| name.Get.ToCharArray ()
       ) ==> Database.DatabasePathHelper.MemoryOrLocalPath(name.Get).EndsWith(name.Get + ".sqlite")
       *)false
    
    //TODO: Check that the prepended path is appropriate for the platform