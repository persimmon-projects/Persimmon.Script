namespace Persimmon

open System
open System.Reflection
open System.Diagnostics
open System.Text.RegularExpressions
open Persimmon
open Persimmon.Internals
open ActivePatterns
open Persimmon.Output

type ScriptContext (watch: Stopwatch, reporter: Reporter) =

  let report result =
    reporter.ReportProgress(TestResult.endMarker)
    reporter.ReportSummary(result.Results)

  let onFinished = ref report

  new() =
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

  member __.OnFinished with get() = !onFinished and set(value) = onFinished := value

  member this.Run(f: ScriptContext -> #seq<#TestMetadata>) =
    let tests = f this
    let runner = TestRunner()
    watch.Start()
    let results = runner.RunSynchronouslyAllTests(reporter.ReportProgress, TestFilter.make TestFilter.allPass, tests)  
    watch.Stop()
    results |> this.OnFinished

  member this.CollectAndRun(f: ScriptContext -> Assembly) =
    let collector = TestCollector()
    this.Run(f >> Seq.singleton >> Seq.collect collector.Collect)

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

  let inline run f (ctx: ScriptContext) =
    ctx.Run(f)

  let inline collectAndRun f (ctx: ScriptContext) =
    ctx.CollectAndRun(f)

module FSI =

  let private tryFind = function
  | Context context ->
    match context.Name with
    | Some name ->
      let m = Regex.Match(name, "FSI_([0-9]+)")
      if m.Success then
        match Int32.TryParse(m.Groups.[1].Value) with
        | true, n -> Some(n, context :> TestMetadata)
        | false, _ -> None
      else None
    | None -> None
  | _ -> None

  let run (f: ScriptContext -> #seq<#TestMetadata>) (ctx: ScriptContext) =

    ctx.Run(
      f
      >> Seq.choose tryFind
      >> Seq.maxBy fst
      >> snd
      >> Seq.singleton
    )

  let collectAndRun f (ctx: ScriptContext) =
    let collector = TestCollector()
    ctx
    |> run (f >> Seq.singleton >>  Seq.collect collector.Collect)
