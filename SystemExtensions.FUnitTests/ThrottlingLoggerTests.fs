module ThrottlingLoggerTests

open System
open System.Diagnostics
open FsUnit
open NUnit.Framework
open Psns.Common.SystemExtensions.Diagnostics

type lib = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule

let stateMachine state = Func<ErrorLoggingState> (fun () -> state)
let getLog machine (messages: (TraceEventType * string) list ref) =
    (new Log(fun msg -> fun _ -> fun eType -> messages := (eType, msg) :: !messages)).WithThrottling(machine)
let normalLog (messages: (TraceEventType * string) list ref) = stateMachine >> getLog <| lib.Normal() <| messages
let errorCount eType items = items |> List.filter (fun t -> fst t = eType) |> List.length

[<Test>]
let ``it should log if the event type is not Error.`` () =
    let messages = ref []
    normalLog(messages).Info("info")
    !messages |> errorCount TraceEventType.Information |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is normal.`` () =
    let messages = ref []
    normalLog(messages).Error("fail")
    !messages |> errorCount TraceEventType.Error |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is saturating.`` () =
    let messages = ref []
    (stateMachine >> getLog <| lib.Normal().Saturating() <| messages).Error("fail")
    !messages |> errorCount TraceEventType.Error |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is normalizing.`` () =
    let messages = ref []
    (stateMachine >> getLog <| lib.Normal().Saturating().AsSaturated().Normalizing() <| messages).Error("fail")
    !messages |> errorCount TraceEventType.Error |> should equal 1

[<Test>]
let ``it should not log if the event type is an Error and state is not normal.`` () =
    let machine = lib.Normal().AsSaturated() |> stateMachine
    let messages = ref []
    let log = getLog machine messages
    log.Error("fail")
    !messages |> errorCount TraceEventType.Error |> should equal 0