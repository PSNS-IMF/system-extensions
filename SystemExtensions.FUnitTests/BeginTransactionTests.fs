module BeginTransactionTests

open System
open System.Data
open Psns.Common.Functional
open Foq
open NUnit.Framework
open FsUnit

type ext = Psns.Common.Functional.Prelude
type db = Psns.Common.SystemExtensions.Database.Prelude

let ok _ = "result"
let fail _ = failwith "failure"
let funFactory run = Func<IDbTransaction, string> run

let createConnection (trans: IDbTransaction) =
    Mock<IDbConnection>().Setup(fun conn -> <@ conn.BeginTransaction() @>).Returns(trans).Create()

let eval factory map createConn =
    db.BeginTransaction().Invoke(createConn, factory map).Match((fun s -> s), (fun e -> e.Message))

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