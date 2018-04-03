module BeginTransactionTests

open System
open System.Data
open Psns.Common.Functional
open Foq
open NUnit.Framework
open FsUnit
open System.Threading.Tasks

type ext = Psns.Common.Functional.Prelude
type db = Psns.Common.SystemExtensions.Database.Prelude

let ok _ = ext.Try(fun () -> "result")
let fail _ = ext.Try(fun () -> (failwith "failure"))
let funFactory run = Func<IDbTransaction, Try<string>> run

let createConnection (trans: IDbTransaction) =
    Mock<IDbConnection>().Setup(fun conn -> <@ conn.BeginTransaction() @>).Returns(trans).Create()

let eval factory map createConn =
    db.BeginTransaction().Invoke(factory map, createConn).Match((fun s -> s), (fun e -> e.Message))

let beginOk map = eval funFactory map << createConnection
let evalOk = beginOk ok
let evalFail = beginOk fail
    
[<Test>]
let ``it should return the result of the given function.`` () =
    let transaction = Mock<IDbTransaction>().Create()

    evalOk transaction |> should equal "result"
    verify <@ transaction.Dispose() @> once

[<Test>]
let ``it should return error text if BeginTransaction throws.`` () =
    let connection = Mock<IDbConnection>().Setup(fun conn -> <@ conn.BeginTransaction() @>).Raises(exn "failure").Create()
    connection |> eval funFactory ok |> should equal "failure"

[<Test>]
let ``it should return error text if action throws.`` () =
    let transaction = Mock<IDbTransaction>().Create()

    evalFail transaction |> should equal "failure"
    verify <@ transaction.Dispose() @> once

// Async tests
let map = Func<IDbTransaction, TryAsync<string>>(fun _ -> ext.TryAsync(fun () -> Task.FromResult "result"))
let evalAsync conn = db.BeginTransactionAsync().Invoke(map, conn).Match((fun s -> s), (fun e -> e.Message)).Result

[<Test>]
let ``it should return the async result of then given function.`` () =
    let transaction = Mock<IDbTransaction>().Create()
    evalAsync (createConnection transaction) |> should equal "result"

[<Test>]
let ``it should return error text if BeginTransactionAsync throws.`` () =
    let connection = Mock<IDbConnection>().Setup(fun conn -> <@ conn.BeginTransaction() @>).Raises(exn "failure").Create()
    evalAsync connection |> should equal "failure"