﻿module CreateCommandTests

open System
open System.Data
open Psns.Common.Functional
open Foq
open NUnit.Framework
open FsUnit

type ext = Psns.Common.Functional.Prelude
type db = Psns.Common.SystemExtensions.Database.Prelude

let ok _ = "result"
let fail _ = failwith "fail"

let action run = Func<IDbCommand, string> run

let eval run trans = db.CreateCommand().Invoke(trans, run).Match((fun s -> s), (fun e -> e.Message))

let createOk map = eval (action map)
let evalOk = createOk ok
let evalFail = createOk fail

[<Test>]
let ``it should return the result of the given function.`` () =
    let command = Mock<IDbCommand>().Create()
    let connection = Mock<IDbConnection>().Setup(fun conn -> <@ conn.CreateCommand() @>).Returns(command).Create()
    let transaction = Mock<IDbTransaction>().Setup(fun trans -> <@ trans.Connection @>).Returns(connection).Create()
 
    evalOk transaction |> should equal "result"
    
    verify <@ connection.CreateCommand() @> once
    verify <@ command.Transaction <- transaction @> once
    verify <@ command.Dispose() @> once

[<Test>]
let ``it should return error text if CreateCommand throws.`` () =
    let connection = Mock<IDbConnection>().Setup(fun conn -> <@ conn.CreateCommand() @>).Raises(exn "fail").Create()
    let transaction = Mock<IDbTransaction>().Setup(fun trans -> <@ trans.Connection @>).Returns(connection).Create()

    evalOk transaction |> should equal "fail"

    verify <@ connection.CreateCommand() @> once

[<Test>]
let ``it should return error text if given function throws.`` () =
    let command = Mock<IDbCommand>().Create()
    let connection = Mock<IDbConnection>().Setup(fun conn -> <@ conn.CreateCommand() @>).Returns(command).Create()
    let transaction = Mock<IDbTransaction>().Setup(fun trans -> <@ trans.Connection @>).Returns(connection).Create()

    evalFail transaction |> should equal "fail"

    verify <@ connection.CreateCommand() @> once
    verify <@ command.Transaction <- transaction @> once
    verify <@ command.Dispose() @> once