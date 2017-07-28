namespace Persimmon

open System
open System.IO
open System.Reflection
open System.Diagnostics
open Persimmon
open Persimmon.Internals
open Persimmon.ActivePatterns
open Persimmon.Runner
open Persimmon.Output

type ScriptContext (watch: Stopwatch, reporter: Reporter) =

  let report result =
    reporter.ReportProgress(TestResult.endMarker)
    reporter.ReportSummary(result.Results)

  let onFinished = ref report

  internal new() =
    let watch = Stopwatch()
    new ScriptContext(watch)

  new(watch) =
    let console = {
      Writer = Console.Out
      Formatter = Formatter.SummaryFormatter.normal watch
    }
    let reporter =
      new Reporter(
        new Printer<_>(Console.Out, Formatter.ProgressFormatter.dot),
        new Printer<_>([console]),
        new Printer<_>(Console.Error, Formatter.ErrorFormatter.normal)
      )
    new ScriptContext(watch, reporter)

  member this.OnFinished with get() = !onFinished and set(value) = onFinished := value

  member this.Run(f: ScriptContext -> #seq<#TestMetadata>) =
    let tests = f this
    watch.Start()
    let results =
      tests
      |> TestRunner.runAllTests reporter.ReportProgress
    watch.Stop()
    results |> this.OnFinished

  member this.CollectAndRun(f: ScriptContext -> Assembly) =
    this.Run(f >> Seq.singleton >> Runner.TestCollector.collectRootTestObjects)

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

  let collectAndRun f =
    use ctx = new ScriptContext()
    ctx.CollectAndRun(f)

  let testReport (f: ScriptContext -> Assembly) : string =
    let stopwatch = Stopwatch()
    use writer = new StringWriter()
    use reporter =
      new Reporter(
        new Printer<_>(TextWriter.Null, Formatter.ProgressFormatter.dot),
        new Printer<_>(writer, Formatter.SummaryFormatter.normal stopwatch),
        new Printer<_>(writer, Formatter.ErrorFormatter.normal)
      )
    use ctx = new ScriptContext(stopwatch, reporter)
    ctx.CollectAndlRun(f)
    writer.ToString()
