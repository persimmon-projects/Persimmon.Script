namespace Persimmon

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions
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

module ScriptFSI =

  let private causesToStrs (causes: (int option * NotPassedCause) seq) =
    causes
      |> Seq.mapi (fun i (l, (Skipped c | Violated c)) -> (i + 1, l,  c))
      |> Seq.collect (fun (i, l, c) ->
        seq {
          match c.Split([|"\r\n";"\r";"\n"|], StringSplitOptions.None) |> Array.toList with
            | [] -> yield ""
            | x::xs ->
              let no = (string i) + ". "
              let x, xs =
               match l with
               | Some l -> (sprintf "Line Number: %d" l, x::xs)
               | None -> (x, xs)
              yield x
              yield! xs |> Seq.map (fun x -> x)
        })

  let rec private toStrs = function
    | EndMarker ->
      Seq.empty
    | ContextResult contextResult ->
      let rs = contextResult.Results |> Seq.collect toStrs
      if Seq.isEmpty rs then
        seq["pass"]
      else
        seq {
          yield contextResult.Context.DisplayName
          yield! rs
        }
    | TestResult testResult ->
      testResult.AssertionResults
      |> Seq.choose (fun ar ->
        match ar.Status with
        | None -> None
        | Some cause -> Some (ar.LineNumber, cause)
      )
      |> causesToStrs

  let private formatter =
    { new IFormatter<ResultNode seq> with
      member x.Format(results: ResultNode seq): IWritable =
        Writable.stringSeq (
            results |> Seq.collect toStrs
        )
    }

  let private writer = new StringWriter()

  let private summaryPrinter = {
    Writer = writer
    Formatter = formatter
  }

  let private reporter =
    new Reporter(
      new Printer<_>(TextWriter.Null, Formatter.ProgressFormatter.dot),
      new Printer<_>([summaryPrinter]),
      new Printer<_>(writer, Formatter.ErrorFormatter.normal)
    )


  let private stopwatch = Stopwatch()

  let private context = new ScriptContext(stopwatch, reporter)

  let public outputFSI AssemblyGetExecutingAssembly =
    context.CollectAndRun(fun _ -> AssemblyGetExecutingAssembly )
    let s = writer.ToString()

    let maxNumberFSI_Spaces =
      s.Split('\n')
      |> Array.filter ( fun s -> Regex.IsMatch(s,"FSI_.*") )
      |> Array.map ( fun s -> s.Substring(4,4))
      |> Array.distinct
      |> fun ary ->
        if Array.isEmpty ary then
          None
        else
          Some (Array.last ary)

    let pos =
      if Option.isNone maxNumberFSI_Spaces then
        stdout.WriteLine("ALL PASS")
        None
      else
        s.Split('\n')
        |> Array.tryFindIndex( fun s -> s.Contains("FSI_" + maxNumberFSI_Spaces.Value ))

    if Option.isSome pos then
      s.Split('\n')
      |> Array.splitAt pos.Value
      |> fun (a,b) ->
        if b.[ Array.length b - 4 ] = "pass" then
          stdout.WriteLine("ALL PASS")
        else
          b |> Array.iter ( fun s -> if s <> "pass" then stdout.WriteLine(s) )
