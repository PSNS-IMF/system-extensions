module IEnumerableTests

open System
open Psns.Common.Functional
open Psns.Common.SystemExtensions
open NUnit.Framework
open FsUnit
open System.Threading
open System.Threading.Tasks

[<Test>]
let ``it should wait for all child tasks when iterating asynchronously.`` () =
    let keys = [ 1..9 ] 
    let mutable items = keys |> List.map (fun i -> (i, 0)) |> Map.ofList

    let rnd = System.Random()
    let locker = obj
    let action =
        let order = ref 0
        Action<int> (fun i ->
            Thread.Sleep(rnd.Next(500))
            lock locker (fun _ -> items <- items.Remove(i).Add(i, Interlocked.Increment(order))))

    keys.IterAsync(action, Maybe<CancellationToken>.None, Maybe<TaskScheduler>.None).Result |> ignore

    let itemList = items |> Map.toList
    let ordered = keys |> List.map (fun i -> (i, i))
    let comparer = List.compareWith(fun v1 v2 -> snd v1 - snd v2)

    comparer ordered itemList |> should not' (equal 0)
    Map.fold (fun state _ value -> state + value) 0 items  |> should equal 45