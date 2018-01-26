module DatabaseTests

open System
open System.Data
open System.Threading.Tasks
open Microsoft.VisualStudio.TestTools.UnitTesting
open LanguageExt
open Foq
open FsUnit.MsTest

open Psns.Common.SystemExtensions.Database

type ext = LanguageExt.Prelude
type db = Psns.Common.SystemExtensions.Database.Prelude

[<TestClass>]
type ``when connecting`` () as self =

    let asString (e : Either<exn, string>) = e.Match((fun s -> s), (fun e -> e.Message))
    let mockConnection = Mock<IDbConnection>().Create()

    member this.run(?buildConn0 : Func<Either<Exception, string>>, ?factory0 : Func<string, IDbConnection>, ?openAsync0 : Func<IDbConnection, Task<IDbConnection>>, ?action0 : Func<IDbConnection, Task<Either<Exception, string>>>) =
        let buildConnectionString = defaultArg buildConn0 (Func<Either<Exception, string>>(fun () -> ext.Right<Exception, string>("string")))
        let connectionFactory = defaultArg factory0 (Func<string, IDbConnection>(fun _ -> mockConnection))
        let openAsync = defaultArg openAsync0 (Func<IDbConnection, Task<IDbConnection>>(fun _ -> mockConnection.Open() |> fun _ -> Task.FromResult mockConnection))
        let action = defaultArg action0 (Func<IDbConnection, Task<Either<Exception, string>>>(fun _ -> Task.FromResult (ext.Right<Exception, string>("result"))))

        asString (db.ConnectAsync(buildConnectionString, connectionFactory, openAsync, action).Result)

    [<TestMethod>] 
    member this.``it should return the result of the given action function.`` () =
        self.run() |> should equal "result"
        Mock.Verify(<@ mockConnection.Open() @>, once)
        Mock.Verify(<@ mockConnection.Dispose() @>, once)

    [<TestMethod>] 
    member this.``it should return error text if buildConnectionString fails.`` () =
        let buildConnFail = Func<Either<Exception, string>>(fun () -> ext.Left<Exception, string>(exn "failure"))
        self.run(buildConn0 = buildConnFail) |> should equal "failure"

    [<TestMethod>] 
    member this.``it should return error text if openAsync throws.`` () =
        let openAsyncFail = Func<IDbConnection, Task<IDbConnection>>(fun _ -> failwith "failure")
        self.run(openAsync0 = openAsyncFail) |> should equal "failure"
        Mock.Verify(<@ mockConnection.Dispose() @>, once)