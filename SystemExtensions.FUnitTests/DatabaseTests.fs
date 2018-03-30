module DatabaseTests

open System
open System.Data
open System.Threading.Tasks
open Psns.Common.Functional
open Foq
open NUnit.Framework
open FsUnit

type ext = Psns.Common.Functional.Prelude
type db = Psns.Common.SystemExtensions.Database.Prelude

// Connect Tests
let factory connection = Func<IDbConnection>(fun () -> connection)
let funFactory map = Func<IDbConnection, string> map

let ok _ = "result"
let fail _ = failwith "fail"

let eval map conn = db.Connect().Invoke(factory conn, funFactory map).Match((fun s -> s), (fun e -> e.Message))

let evalOk = eval ok
let evalFail = eval fail

[<Test>]
let ``it should return the result of the given function.`` () =
    let connection = Mock<IDbConnection>().Create()

    evalOk connection |> should equal "result"
    verify <@ connection.Dispose() @> once

[<Test>]
let ``it should return error text if action fails.`` () =
    let connection = Mock<IDbConnection>().Create()

    evalFail connection |> should equal "fail"
    verify <@ connection.Dispose() @> once

// ConnectAsync Tests
let asTask func = func >> Task.FromResult
let openAsync func = Func<IDbConnection, Task<IDbConnection>> func
let openOk = openAsync (asTask (fun cn -> cn))
let openFail = openAsync (fun _ -> failwith "fail")

let connectAsync conn openAsync map =
    db.ConnectAsync().Invoke(factory conn, openAsync, Func<IDbConnection, Task<string>> map)

let evalAsync openAsync map conn = (connectAsync conn openAsync (asTask map)).Match((fun s -> s), (fun e -> e.Message)).Result

let evalOkAsync = evalAsync openOk ok
let evalFailAsync = evalAsync openOk fail
let openAsyncFail = evalAsync openFail ok

[<Test>]
let ``it should return the result of the given async function.`` () =
    let connection = Mock<IDbConnection>().Create()

    evalOkAsync connection |> should equal "result"
    verify <@ connection.Dispose() @> once

[<Test>]
let ``it should return error text if openAsync fails.`` () =
    let connection = Mock<IDbConnection>().Create()

    openAsyncFail connection |> should equal "fail"
    verify <@ connection.Dispose() @> once

[<Test>]
let ``it should return error text if action async fails.`` () =
    let connection = Mock<IDbConnection>().Create()

    evalFailAsync connection |> should equal "fail"
    verify <@ connection.Dispose() @> once