module Persimmon.Tests.FSITest

open Persimmon
open UseTestNameByReflection

let ``run only latest namespace`` =

  let tests =
    [0..1]
    |> List.map (fun n ->
      Context(
        sprintf "FSI_%04d" n,
        parameterize {
          source [n]
          run (fun n -> test { return n })
        }
        |> Seq.map (fun x -> x :> TestMetadata)
      )
    )
  test {
    let count = ref 0
    use ctx = new ScriptContext()
    ctx
    |> FSI.run (fun ctx ->
      ctx.OnFinished <- fun x -> count := Script.countPassedOrSkipped x
      tests
    )
    do! assertEquals 1 !count
  }
