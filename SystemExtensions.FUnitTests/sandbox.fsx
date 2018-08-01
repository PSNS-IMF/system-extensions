#r "bin/Debug/System.ValueTuple.dll"
#r "bin/Debug/Psns.Common.Functional.dll"
#r "bin/Debug/Psns.Common.Analysis.dll"
#r "bin/Debug/Psns.Common.SystemExtensions.dll"
#load "aliases.fs"

open System
open Psns.Common.Analysis
open Psns.Common.Functional
open SystemExtensions.FUnitTests

open aliases

type ext = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

let classify =
    Lib.memoizePrev (Func<_, _>(fun prev -> prev |> function
    | Classification.Norm -> classHigh
    | _ -> classNorm)) classHigh
let state = ext.ErrorRateStateMachine.Par(classify)

let runMachine = Func<(ErrorStateTransition * ErrorState), (ErrorStateTransition * ErrorState)>(fun last ->
    let (_, st) = state.Invoke().Invoke(snd last).ToTuple()
    let transition = 
        match (snd last, st) with
        | (ErrorState.Normal, ErrorState.Saturated) -> saturating
        | (ErrorState.Saturated, ErrorState.Normal) -> normalizing
        | _ -> ErrorStateTransition.None
    (transition, st))

let getState = Lib.memoPrevWithReader runMachine (noTransition, normal) |> fun func -> func.Invoke() |> fst

Seq.replicate 3 getState.Invoke 
|> Seq.iter (fun get ->
    let (t, s) = get()
    printfn "%O %O" t s)