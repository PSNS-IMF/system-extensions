module IEnumerableTests

open System
open Psns.Common.Functional
open Psns.Common.SystemExtensions
open NUnit.Framework
open FsUnit
open System.Threading
open System.Threading.Tasks

let keys = [ 1..9 ]
let createItems () = keys |> List.map (fun i -> (i, 0)) |> Map.ofList

let rnd = System.Random()
let locker = obj

let action failOn0 (items: Map<int, int> ref) =
    let mutable order = 0
    let fail index = 
        match failOn0 with
        | Some(failOn) when failOn = index -> true
        | _ -> false
    Action<int> (fun i ->
            match fail i with
            | true -> failwith "fail"
            | _ -> 
                Thread.Sleep(rnd.Next(200))
                lock locker (fun () -> 
                    items := items.Value.Remove(i).Add(i, Interlocked.Increment &order)))

let actionOk = action None

let oneToOne = keys |> List.map (fun i -> (i, i))
let comparer = List.compareWith(fun v1 v2 -> snd v1 - snd v2)
let compareWithOneToOne items = comparer oneToOne (Map.toList items)
let reduceValues items = Map.fold (fun state _ value -> state + value) 0 items

let tryIterAsync action = keys.TryIterAsync(action, Maybe<CancellationToken>.None, Maybe<TaskScheduler>.None).Match((fun _ -> "ok"), (fun _ -> "fail")).Result

[<Test>]
let ``it execute a child task for each key concurrently.`` () =
    let items = ref (createItems())

    keys.IterAsync(actionOk items, Maybe<CancellationToken>.None, Maybe<TaskScheduler>.None).Result |> ignore

    // ensure tasks ran in parallel
    compareWithOneToOne !items |> should not' (equal 0)

    // ensure all tasks completed
    reduceValues !items |> should equal 45

[<Test>]
let ``it should return successfully when executing child tasks for each key.`` () =
    let items = ref (createItems())

    tryIterAsync(actionOk items) |> should equal "ok"

    // ensure tasks ran in parallel
    compareWithOneToOne !items |> should not' (equal 0)

    // ensure all tasks completed
    reduceValues !items |> should equal 45

[<Test>]
let ``it should return an error when a child task fails.`` () =
    let items = ref (createItems())

    tryIterAsync(action (Some 2) items) |> should equal "fail"

    // ensure tasks ran in parallel
    compareWithOneToOne !items |> should not' (equal 0)

    // ensure all tasks completed
    reduceValues !items |> should (equalWithin 9) 45