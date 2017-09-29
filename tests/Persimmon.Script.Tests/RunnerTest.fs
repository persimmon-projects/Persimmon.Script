module Persimmon.Tests.RunnerTest

open Persimmon
open UseTestNameByReflection

let ``script tests should pass`` = test {
  let failCount = ref 0
  use ctx = new ScriptContext()
  ctx
  |> Script.run (fun ctx ->
    ctx.OnFinished <- fun x -> failCount := Script.countNotPassedOrError x
    let unit = test {
      do! assertEquals 1 1
    }
    let ``return value`` = test {
      return 1
    }
    [
      unit :> TestMetadata
      ``return value`` :> TestMetadata
    ]
  )
  do! assertEquals 0 !failCount
}

let ``parameterized script tests should pass`` =
  let parameterizeTest x = test {
    do! assertEquals 0 (x % 2)
  }
  test {
    let failCount = ref 0
    use ctx = new ScriptContext()
    ctx
    |> Script.run (fun ctx ->
      ctx.OnFinished <- fun x -> failCount := Script.countNotPassedOrError x
      parameterize {
        case 2
        case 4
        run parameterizeTest
      }
    )
    do! assertEquals 0 !failCount
  }


let ``count tests`` =
  let passed = test { do! assertEquals 0 0 }
  let skipped = test { do! assertEquals 0 1 } |> skip "skip example"
  let notPassed = test { do! assertEquals 0 1 }
  let error = test {
    failwith "error example"
    do! Assert.Fail("expected raise exception, but was passed")
  }
  test {
    let passedOrSkippedCount = ref 0
    use ctx = new ScriptContext()
    ctx
    |> Script.run (fun ctx ->
      ctx.OnFinished <- fun x ->
        passedOrSkippedCount := Script.countPassedOrSkipped x
      [
        passed
        skipped
        notPassed
        error
      ]
    )
    do! assertEquals 2 !passedOrSkippedCount
  }
