#r @"../packages/examples/Persimmon/lib/net45/Persimmon.dll"
#r @"../packages/examples/Persimmon.Runner/lib/net40/Persimmon.Runner.dll"
#r @"../src/Persimmon.Script/bin/Release/net45/Persimmon.Script.dll"

open System.Reflection
open Persimmon
open UseTestNameByReflection

let ``a unit test`` = test {
  do! assertEquals 1 1
}

let ``return value example`` = test {
  return 1
}

let parameterizeTest x = test {
  do! assertEquals 0 (x % 2)
}

let ``parameterize test`` = parameterize {
  case 2
  case 4
  run parameterizeTest
}

let context = new ScriptContext()
context
|> Script.collectAndRun (fun _ -> Assembly.GetExecutingAssembly())
