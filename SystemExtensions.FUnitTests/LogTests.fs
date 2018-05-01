module LogTests

open System
open System.Diagnostics
open NUnit.Framework
open FsUnit
open Psns.Common.Analysis
open Psns.Common.Analysis.Anomaly
open Psns.Common.Functional
open Psns.Common.SystemExtensions.Diagnostics

type ext = Psns.Common.Functional.Prelude
type dg = Psns.Common.SystemExtensions.Diagnostics.Prelude

let getDelta = Func<Tuple<Delta, float>> (fun () -> (Delta.ofValues DateTime.Now TimeSpan.Zero), 1.0)
let composeClassify cType = (Func<Tuple<Delta, float>, Classification> (fun _ -> cType)).Compose getDelta
let getDef () = ref TraceEventType.Information
let getLog (logged: TraceEventType ref) = new Log(fun _ -> fun _ -> fun eType -> logged := eType)
let run (log: Log) classify = (log.UseErrorClassification classify).Error("fail")

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

[<Test>]
let ``it should do what I say.`` () =
    let logged = ref []
    let log = new Log(fun _ -> fun _ -> fun eType -> logged := List.Cons (eType, !logged))
    let defDelta = new Delta()
    let getDelta =
        let call = ref -1
        Func<Tuple<Delta, float>> (fun () ->
            incr call
            let rate = 
                match !call with
                | 0 -> 0
                | 1 -> 6
                | 2 -> 5
                | _ -> 4
                |> float
            defDelta, rate)
    let classifyDelta = Func<Tuple<Delta, float>, Classification> (fun delta -> if (snd delta) > 5.0 then Norm else High)
    let classify = classifyDelta.Compose getDelta
    let tLog = log. UseErrorClassification classify

    List.iter (fun _ -> tLog.Error ("fail")) [1..4]

    !logged |> List.filter (fun t -> t = TraceEventType.Error) |> List.length |> should equal 1