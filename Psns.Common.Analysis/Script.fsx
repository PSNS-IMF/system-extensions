// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

#load "Library1.fs"
open Psns.Common.Analysis
open System

// Define your library scripting code here

let calcDelta (optionalPrevious: DateTime option) =
    match optionalPrevious with
    | Some(prev) -> DateTime.Now.Subtract(prev).Ticks
    | None -> TimeSpan.Zero.Ticks

let apply delta = (delta * 0.001, delta * 0.999)

// calcDelta |> applyBoundary |> classify