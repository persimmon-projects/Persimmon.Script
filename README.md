# Persimmon.Script

Persimmon.Script is a script helper for Persimmon.

## Usage

```fsharp
#r @"../packages/examples/Persimmon/lib/net45/Persimmon.dll"
#r @"../packages/examples/Persimmon.Runner/lib/net40/Persimmon.Runner.dll"
#r @"../src/Persimmon.Script/bin/Release/net45/Persimmon.Script.dll"

// collect test from Assembly and run tests
Script.collectAndRun (fun _ -> Assembly.GetExecutingAssembly())
```
