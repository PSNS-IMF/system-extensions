module IEnumerableTests

open NUnit.Framework
open Psns.Common.SystemExtensions
open FsUnit

[<Test>] 
let ``it should divide an IEnumerable into properly sized chunks.`` () =
    let chunks = [1..1000].Chunk()

    Seq.length chunks |> should equal 2
    chunks |> Seq.item 0 |> Seq.length |> should equal 500
    chunks |> Seq.item 1 |> Seq.length |> should equal 500

    let odds = [1..493].Chunk(101)

    Seq.length odds |> should equal 5
    Seq.iter (fun i -> odds |> Seq.item i |> Seq.length |> should equal 101) [0..3]
    Seq.item 4 odds |> Seq.length |> should equal 89

[<Test>] 
let ``it should divide an IEnumerable into properly sized chunks when empty.`` () =
    let items = Seq.empty
    let chunks = items.Chunk()

    Seq.length chunks |> should equal 0