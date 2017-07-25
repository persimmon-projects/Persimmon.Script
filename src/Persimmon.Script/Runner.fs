namespace Persimmon

open System
open System.Reflection
open System.Diagnostics
open Persimmon
open Persimmon.Internals
open Persimmon.ActivePatterns
open Persimmon.Runner
open Persimmon.Output

type ScriptContext internal () =
  let watch = Stopwatch()
  let reporter =
    let console = {
      Writer = Console.Out
      Formatter = Formatter.SummaryFormatter.normal watch
    }
    new Reporter(
      new Printer<_>(Console.Out, Formatter.ProgressFormatter.dot),
      new Printer<_>([console]),
      new Printer<_>(Console.Error, Formatter.ErrorFormatter.normal)
    )

  let report result =
    reporter.ReportProgress(TestResult.endMarker)
    reporter.ReportSummary(result.Results)

  let onFinished = ref report

  member this.OnFinished with get() = !onFinished and set(value) = onFinished := value
  member this.Run(f: ScriptContext -> #seq<#TestMetadata>) =
    let tests = f this
    watch.Start()
    let results =
      tests
      |> TestRunner.runAllTests reporter.ReportProgress
    watch.Stop()
    results |> this.OnFinished

  interface IDisposable with
    member __.Dispose() = (reporter :> IDisposable).Dispose()

module Script =

  let countPassedOrSkipped results =
    let rec inner count = function
    | EndMarker -> count
    | ContextResult contextResult ->
      contextResult.Results |> Seq.fold inner count
    | TestResult testResult ->
      if Array.isEmpty testResult.Exceptions then
        match (testResult.AssertionResults |> AssertionResult.Seq.typicalResult).Status with
        | None
        | Some (Skipped _) -> count + 1
        | Some (Violated _) -> count
      else count
    results.Results
    |> Array.fold inner 0 

  let countNotPassedOrError results = results.Errors

  let run f =
    use ctx = new ScriptContext()
    ctx.Run(f)

#if NETSTANDARD
#else
  let collectAndRun () =
    use ctx = new ScriptContext()
    ctx.Run(fun _ ->
      [ Assembly.GetExecutingAssembly() ]
      |> TestCollector.collectRootTestObjects
    )
#endif
