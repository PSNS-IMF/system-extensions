module LogTests

open System
open System.Diagnostics
open NUnit.Framework
open FsUnit
open Psns.Common.Analysis
open Psns.Common.Analysis.Anomaly
open Psns.Common.SystemExtensions.Diagnostics

type ext = Psns.Common.Functional.Prelude
type dg = Psns.Common.SystemExtensions.Diagnostics.Prelude

let getDelta = Func<Tuple<Delta, float>> (fun () -> (Delta.ofValues DateTime.Now TimeSpan.Zero), 1.0)
let mapRate = Func<Tuple<Delta, float>, Boundary<float>> (fun _ -> Boundary.ofValues (0.0, 0.0))
let composeClassify cType = Func<Boundary<float>, float, Classification> (fun _ _ -> cType)
let getDef () = ref TraceEventType.Information
let getLog (logged: TraceEventType ref) = new Log(fun _ -> fun _ -> fun eType -> logged := eType)
let run (log: Log) classify = (log.UseErrorThrottling (getDelta, mapRate, classify)).Error("fail")

[<Test>]
let ``it should not log a High anomaly when throttling errors.`` () =
    let logged = getDef ()
    let log = getLog logged
    let classify = composeClassify Classification.High

    run log classify |> ignore


    !logged |> should equal TraceEventType.Verbose

[<Test>]
let ``it should not log a Low anomaly when throttling errors.`` () =
    let logged = getDef ()
    let log = getLog logged
    let classify = composeClassify Classification.Low

    run log classify |> ignore

    !logged |> should equal TraceEventType.Verbose

[<Test>]
let ``it should log a non-anomaly error when throttling errors.`` () =
    let logged = getDef ()
    let log = getLog logged
    let classify = composeClassify Classification.Norm

    run log classify |> ignore

    !logged |> should equal TraceEventType.Error