module ErrorRateStateTests

open System
open NUnit.Framework
open FsUnit
open Psns.Common.Analysis.Anomaly
open Psns.Common.Analysis
open Psns.Common.Functional
open Psns.Common.SystemExtensions.Diagnostics
open SystemExtensions.FUnitTests

open aliases

type err = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

let classNorm = Func<_>(fun _ -> Classification.Norm)
let classHigh = Func<_>(fun _ -> Classification.High)

type Machine(?classify0: Func<Classification>) =
    let classify = defaultArg classify0 classNorm

    member __.Create() =
        err.ErrorRateStateMachine.Par(classify).Invoke()

[<TestFixture>]
type ``when classification is normal`` () =
    let machineFactory initialState =
        Machine().Create().Invoke initialState

    [<Test>]
    member __.``it should set then state to normal when previous state is saturated.`` () =
        let struct (cls, state) = machineFactory saturated

        cls |> should equal Classification.Norm
        state |> should equal normal

    [<Test>]
    member __.``it should set then state to normal when previous state is normal.`` () =
        let struct (cls, state) = machineFactory normal

        cls |> should equal Classification.Norm
        state |> should equal normal

[<TestFixture>]
type ``when classification is high`` () =
    let machineFactory initialState =
        Machine(classHigh).Create().Invoke initialState

    [<Test>]
    member __.``it should set then state to saturated when previous state is normal.`` () =
        let struct (cls, state) = machineFactory normal

        cls |> should equal Classification.High
        state |> should equal saturated

    [<Test>]
    member __.``it should set then state to saturated when previous state is saturated.`` () =
        let struct (cls, state) = machineFactory saturated

        cls |> should equal Classification.High
        state |> should equal saturated

[<Test>]
let ``it should alternate states correctly.`` () =
    let prevFunc = Func<_, _>(fun prev -> prev |> function
        | Classification.Norm -> Classification.High 
        | _ -> Classification.Norm)
    let classify = Lib.memoizePrev prevFunc Classification.High

    let machine () = Machine(classify).Create()
    let history = machine().Bind(machine())
    
    let struct (_, state) = history.Invoke(normal)
    
    state |> should equal saturated

    let anotherChange = history.Bind(machine())
    let struct (_, state2) = anotherChange.Invoke(normal)

    state2 |> should equal normal