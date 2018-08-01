module ThrottlingLoggerTests

open System
open System.Diagnostics
open FsUnit
open NUnit.Framework
open Psns.Common.SystemExtensions.Diagnostics
open SystemExtensions.FUnitTests

open aliases

let stateMachine transitionState = Func<string, struct (ErrorStateTransition * ErrorState)> (fun _ -> transitionState)
let getLog machine (messages: (TraceEventType * string) list ref) =
    (new Log(fun msg -> fun _ -> fun eType -> messages := (eType, msg) :: !messages)).WithThrottling(machine)
let log (messages: (TraceEventType * string) list ref) (transitionState: struct (ErrorStateTransition * ErrorState)) =
    (stateMachine >> getLog <| transitionState <| messages)
let normalLog messages = log messages struct (noTransition, normal)
let info (log: Log) = log.Info("info")
let error (log: Log) = log.Error("fail")
let errorCount eType items = items |> List.filter (fun t -> fst t = eType) |> List.length

[<Test>]
let ``it should log if the event type is not Error.`` () =
    let messages = ref []
    normalLog messages |> info
    !messages |> errorCount TraceEventType.Information |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is normal.`` () =
    let messages = ref []
    normalLog messages |> error
    !messages |> errorCount TraceEventType.Error |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is saturating.`` () =
    let messages = ref []
    struct (saturating, saturated) |> log messages |> error
    !messages |> errorCount TraceEventType.Error |> should equal 1

[<Test>]
let ``it should log if the event type is an Error and state is normalizing.`` () =
    let messages = ref []
    struct (normalizing, normal) |> log messages |> error
    !messages |> errorCount TraceEventType.Error |> should equal 2

[<Test>]
let ``it should not log if the event type is an Error and state is not normal.`` () =
    let messages = ref []
    struct (noTransition, saturated) |> log messages |> error
    !messages |> errorCount TraceEventType.Error |> should equal 0