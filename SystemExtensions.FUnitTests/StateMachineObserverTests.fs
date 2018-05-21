module StateMachineObserverTests

open System
open System.Threading.Tasks
open FsUnit
open NUnit.Framework
open Psns.Common.SystemExtensions.Diagnostics

type pre = Psns.Common.Functional.Prelude
type ext = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

let sendMail (wasCalled:bool ref) = Func<string, Task * int>(fun _ ->
    wasCalled := true
    pre.UnitTask.Invoke(), 0)

let stateMachine state = Func<ErrorLoggingState>(fun () -> state)
let observer = ext.StateMachineObserver()
let observe state wasCalled = observer.Invoke (sendMail wasCalled, stateMachine state)

[<SetUp>]
let ``execute once because first execution is skipped.`` () =
    observe (ext.Normal().Saturating()) (ref false) |> ignore

[<Test>]
let ``it should send an email when then state is saturating.`` () =
    let called = ref false
    observe (ext.Normal().Saturating()) called |> ignore
    !called |> should be True

[<Test>]
let ``it should not send an email when then state is any other state.`` () =
    Seq.iter (fun state ->
        let called = ref false
        observe state called |> ignore
        !called |> should be False)
        [ext.Normal();ext.Normal().AsSaturated()]