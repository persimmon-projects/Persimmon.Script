module Persimmon.Tests.RunnerTest

open Persimmon
open UseTestNameByReflection

let ``script tests should pass`` = test {
  let failCount = ref 0
  Runner.run begin fun ctx ->
    ctx.OnFinished <- fun x -> failCount := Runner.countNotPassedOrError x
    [
      ctx.test "unit" {
        do! assertEquals 1 1
      }
      ctx.test "return value" {
        return 1
      }
    ]
  end
  do! assertEquals 0 !failCount
}

let ``parameterized script tests should pass`` =
  let parameterizeTest x = test {
    do! assertEquals 0 (x % 2)
  }
  test {
    let failCount = ref 0
    Runner.run begin fun ctx ->
      ctx.OnFinished <- fun x -> failCount := Runner.countNotPassedOrError x
      ctx.parameterize {
        case 2
        case 4
        run parameterizeTest
      }
    end
    do! assertEquals 0 !failCount
  }

