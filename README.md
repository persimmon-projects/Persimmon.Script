# Persimmon.Script
[![NuGet Status](http://img.shields.io/nuget/v/Persimmon.Script.svg?style=flat)](https://www.nuget.org/packages/Persimmon.Script/)
[![Build status](https://ci.appveyor.com/api/projects/status/htqrush74ejmowh0/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/persimmon-script/branch/master)
[![Build Status](https://travis-ci.org/persimmon-projects/Persimmon.Script.svg?branch=master)](https://travis-ci.org/persimmon-projects/Persimmon.Script)

Persimmon.Script is a script helper for Persimmon.

## Usage

```fsharp
#r @"./packages/Persimmon/lib/net45/Persimmon.dll"
#r @"./packages/Persimmon.Runner/lib/net40/Persimmon.Runner.dll"
#r @"./packages/Persimmon.Script/net45/Persimmon.Script.dll"

open UseTestNameByReflection
open System.Reflection
open Persimmon

// write tests ...
let ``a unit test`` = test {
  do! assertEquals 1 2
}

// collect test from Assembly and run tests
// ===== case ( FSharp Interactive ) =====
new ScriptContext()
|> FSI.collectAndRun( fun _ -> Assembly.GetExecutingAssembly() )

// ===== case ( Othres ) =====
new ScriptContext()
|> Script.collectAndRun( fun _ -> Assembly.GetExecutingAssembly() )
```

If you want to use your reporter:

```fsharp
open System.IO
open System.Diagnostics
open System.Reflection
open Persimmon
open Persimmon.Output

// write tests ...

let stopwatch = Stopwatch()

// define original reporter
use writer = new StringWriter()
let summaryPrinter = {
  Writer = writer
  Formatter = Formatter.SummaryFormatter.normal stopwatch
}
use reporter =
  new Reporter(
    new Printer<_>(TextWriter.Null, Formatter.ProgressFormatter.dot),
    new Printer<_>([summaryPrinter]),
    new Printer<_>(writer, Formatter.ErrorFormatter.normal)
  )

use context = new ScriptContext(stopwatch, reporter)
context.CollectAndRun(fun _ -> Assembly.GetExecutingAssembly())
```

