module StateMachineObserverTests

open System
open System.Threading.Tasks
open FsUnit
open NUnit.Framework
open SystemExtensions.FUnitTests

open aliases

type pre = Psns.Common.Functional.Prelude
type ext = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

[<TestFixture>]
type ``when observing state transitions`` () =
    let sendMail (wasCalled:bool ref) = Func<string, string, Task>(fun _ _ ->
        wasCalled := true
        pre.UnitTask.Invoke())

    let getState (transition: ErrorStateTransition) =
        Func<_>(fun () -> struct (transition, normal))

    let observer () = ext.StateTransitionObserver
    let observe wasCalled transition =
        observer().Invoke (sendMail wasCalled, getState transition)

    let run response wasCalled =
        (observe wasCalled response).Invoke("error")

    [<Test>]
    member __.``it should send an email when then state is saturating.`` () =
        let called = ref false
        run saturating called |> ignore
        !called |> should be True

    [<Test>]
    member __.``it should not send an email when then state is any other state.`` () =
        Seq.iter (fun transition ->
            let called = ref false
            run transition called |> ignore
            !called |> should be False)
            [normalizing;noTransition]

[<TestFixture>]
type ``when classifying state transitions`` () = 
    
    let asState state =
        pre.State<Classification, ErrorState>(fun _ -> struct (classNorm, state))

    let asTranState errorState =
        struct (noTransition, errorState)

    let classify endState currentState =
        ext.StateTransitionClassifier.Invoke(endState).Invoke(currentState)

    [<Test>] member __.
      ``it should be saturating when state goes from normal to saturated.`` () =
        asTranState normal
        |> classify (asState saturated)
        |> should equal (struct (saturating, saturated))
        
    [<Test>] member __.
      ``it should be normalizing when state goes from saturated to normal.`` () =
        asTranState saturated
        |> classify (asState normal)
        |> should equal (struct (normalizing, normal))

    [<Test>] member __.
      ``it should be none when state goes from normal to normal.`` () =
        asTranState normal
        |> classify (asState normal)
        |> should equal (struct (noTransition, normal))