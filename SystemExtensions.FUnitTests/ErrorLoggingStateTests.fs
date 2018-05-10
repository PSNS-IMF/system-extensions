module ErrorLoggingStateTests

open System
open NUnit.Framework
open Psns.Common.Analysis.Anomaly
open Psns.Common.Analysis
open Psns.Common.Functional
open Psns.Common.SystemExtensions.Diagnostics
open FsUnit

type lib = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule
type fl = Psns.Common.Functional.Prelude

let call = ref 0
let classify =
    Func<Classification> (
        fun () ->
            let res =
                match !call with
                | 0 -> Norm
                | 1 -> High
                | 2 -> Low
                | 3 -> Norm
                | 4 -> High
                | _ -> Norm
            incr call
            res)
        
let basicLog = new Log(fun _ -> fun _ -> fun _ -> ())

let start = DateTime.Now
let machine = (lib.StateMachineFactory.Par (basicLog, classify)).Invoke(fl.Some start)

let map (a: ErrorLoggingState) =
    List.Cons (a.Normal, [a.SaturatingAck; a.Saturated; a.NormalizingAck])
    |> List.filter (fun m -> m.IsSome)
    |> List.fold (fun state m -> state + m.Match ((fun dt -> dt.Ticks), (fun () -> 0L))) 0L
    
let normal = lib.Normal(fl.Some start)

[<Test>] 
let ``it should first return the correct sequence of states.`` () =
    Seq.iter (fun i ->
                let state = machine.Invoke()
                let expected = match i with
                                | 0 -> normal
                                | 1 -> normal.Saturating()
                                | 2 -> normal.AsSaturated()
                                | 3 -> normal.AsSaturated().Normalizing()
                                | 4 -> normal.AsSaturated().Normalizing().Saturating()
                                | 5 -> normal.AsSaturated().Normalizing().Saturating().Normalizing()
                                | 6 -> normal
                                | _ -> failwith "fail"
                map state |> should equal (map expected)) [0..6]

[<Test>]
let ``it should mark the first non-normal error as Saturating.`` () =
    let errClassify = Func<Classification> (fun () -> High)
    let lMachine = lib.StateMachineFactory.Par (basicLog, errClassify)
    let state = lMachine.Invoke(fl.Some start).Invoke()
    map state |> should equal (map (lib.Normal(fl.Some start).Saturating()))

[<Test>]
let ``it should classify the delta rate correctly.`` () =
    let deltaRate = Func<Delta * float>(fun () -> (Delta.ofValues DateTime.MinValue TimeSpan.Zero), 0.0)
    let applyBoundary = Func<Delta * float, float bound>(fun dRate -> Boundary.ofValues (0.0, 0.0))
    let classify = Func<float bound, float, Classification>(fun b rate -> Classification.High)

    lib.Classify.Invoke (deltaRate, applyBoundary, classify) |> should equal Classification.High

[<Test>]
let ``it should apply the correct Boundary given a rate.`` () =
    let getDelta = Delta.ofValues DateTime.MinValue (TimeSpan.Zero)
    let applyFunc = lib.ApplyBoundary.Invoke 0.1
    let apply rate = applyFunc.Invoke (getDelta, rate)

    apply 2.0 |> should equal (Boundary.ofValues (0.0, 0.0))
    apply infinity |> fun boundary -> boundary.Max |> should equal 1.998
    apply 0.1 |> fun boundary ->
        boundary.Min |> should (equalWithin 0.00011) 0.0001
        boundary.Max |> should (equalWithin 0.01) 0.09

[<Test>]
let ``it should have the correct status.`` () =
    let eval (state: ErrorLoggingState) expecteds =
        List.iter
            (fun i -> 
                let expected = List.item i expecteds
                match i with
                | 0 -> state.IsNormal |> should equal expected
                | 1 -> state.IsSaturating |> should equal expected
                | 2 -> state.IsSaturated |> should equal expected
                | 3 -> state.IsNormalizing |> should equal expected
                | 4 -> state.IsUninitiated |> should equal expected
                | _ -> failwith "match not exhaustive")
            [0..4]

    eval (lib.Normal())                 [true;false;false;false;false]
    eval (lib.Normal().Saturating())    [false;true;false;false;false]
    eval (lib.Normal().AsSaturated())   [false;false;true;false;false]
    eval (lib.Normal().Normalizing())   [false;false;false;true;false]
    eval (new ErrorLoggingState())      [false;false;false;false;true]

    let state = lib.Normal()
    eval (state)                [true;false;false;false;false]
    eval (state.Saturating())   [false;true;false;false;false]
    eval (state.AsSaturated())  [false;false;true;false;false]
    eval (state.Normalizing())  [false;false;false;true;false]
    eval (state.AsNormal())     [true;false;false;false;false]

    let state = lib.Normal().Saturating()
    eval (state)                [false;true;false;false;false]
    eval (state.Saturating())   [false;true;false;false;false]
    eval (state.AsSaturated())  [false;false;true;false;false]
    eval (state.Normalizing())  [false;false;false;true;false]
    eval (state.AsNormal())     [true;false;false;false;false]

    let state = lib.Normal().Saturating().AsSaturated()
    eval (state)                [false;false;true;false;false]
    eval (state.Saturating())   [false;true;false;false;false]
    eval (state.AsSaturated())  [false;false;true;false;false]
    eval (state.Normalizing())  [false;false;true;true;false]
    eval (state.AsNormal())     [true;false;false;false;false]

    let state = lib.Normal().Saturating().AsSaturated().Normalizing()
    eval (state)                [false;false;true;true;false]
    eval (state.Saturating())   [false;true;false;false;false]
    eval (state.AsSaturated())  [false;false;true;false;false]
    eval (state.Normalizing())  [false;false;true;true;false]
    eval (state.AsNormal())     [true;false;false;false;false]

    let state = lib.Normal().Saturating().AsSaturated().Normalizing().AsNormal()
    eval (state)                [true;false;false;false;false]
    eval (state.Saturating())   [false;true;false;false;false]
    eval (state.AsSaturated())  [false;false;true;false;false]
    eval (state.Normalizing())  [false;false;false;true;false]
    eval (state.AsNormal())     [true;false;false;false;false]

    