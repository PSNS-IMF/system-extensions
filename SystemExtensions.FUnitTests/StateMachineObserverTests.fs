module StateMachineObserverTests

open System
open System.Threading.Tasks
open FsUnit
open NUnit.Framework
open SystemExtensions.FUnitTests
open Psns.Common.Analysis.Anomaly

open aliases
open helpers

type pre = Psns.Common.Functional.Prelude
type ext = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

let sendMail _ (wasCalled:bool ref) = Func<string, ErrorState, Task>(fun _ _ ->
    wasCalled := true
    pre.UnitTask.Invoke())

let stateMachine (cls: Classification) (response: ErrorState) =
    pre.State<Classification, ErrorState>(fun _ -> struct (cls, response))

let observer () = ext.StateMachineObserver
let observe wasCalled cls state =
    let msg = "error"
    observer().Invoke (sendMail msg wasCalled, stateMachine cls state, msg)

let run (initial: ErrorState) cls response wasCalled =
    (observe wasCalled cls response).Invoke(initial)

[<Test>]
let ``it should send an email when then state is saturating.`` () =
    let called = ref false
    run normal classHigh saturated called |> ignore
    !called |> should be True

[<Test>]
let ``it should not send an email when then state is any other state.`` () =
    Seq.iter (fun initResponse ->
        let called = ref false
        run (fst initResponse) (snd initResponse) (thd initResponse) called |> ignore
        !called |> should be False)
        [(normal, classNorm, normal);(saturated, classHigh, normal)]