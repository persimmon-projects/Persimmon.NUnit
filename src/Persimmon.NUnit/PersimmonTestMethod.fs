namespace Persimmon.NUnit

open System
open System.IO
open System.Reflection
open System.Threading

open NUnit.Core
open NUnit.Core.Extensibility
open NUnit.Framework

open Persimmon
open Persimmon.ActivePatterns
open Persimmon.Runner
open Persimmon.Output

type PersimmonTestMethod(m: MethodInfo) =
  inherit TestMethod(m)

  override x.RunTest() =
    let testResult = TestResult(x)
    TestExecutionContext.CurrentContext.CurrentResult <- testResult
    try
      try
        x.RunSetUp()
        x.RunTestCase(testResult)
      with
        | ex -> x.HandleException(ex, testResult, FailureSite.SetUp)
    finally
      x.RunTearDown(testResult)
    testResult

  member private x.RunSetUp() =
    if x.setUpMethods <> null then
      x.setUpMethods |> Array.iter x.InvokeMethodIgnore

  member private x.RunTearDown(testResult) =
    try
      if x.tearDownMethods <> null then
        x.tearDownMethods
        |> Array.rev
        |> Array.iter x.InvokeMethodIgnore
    with
      | ex ->
        let tmp =
          match ex with
          | :? NUnitException -> ex.InnerException
          | _ -> ex
        x.RecordException(tmp, testResult, FailureSite.TearDown)

  member private x.InvokeMethodIgnore(m) =
    x.invokeMethod m |> ignore

  member private x.invokeMethod (mi:MethodInfo) =
    Reflect.InvokeMethod(mi, if mi.IsStatic then null else x.Fixture)

  member private x.RunTestCase(testResult) =
    try
      x.RunTestMethod(testResult)
    with
      | ex -> x.HandleException(ex, testResult, FailureSite.Test)

  member private x.RunTestMethod(testResult) =
    // dummy reporter
    use reporter =
        new Reporter(
          new Printer<_>(new StringWriter(), Formatter.ProgressFormatter.dot),
          new Printer<_>(new StringWriter(), Formatter.SummaryFormatter.normal),
          new Printer<_>(new StringWriter(), Formatter.ErrorFormatter.normal))
    let test = x.invokeMethod x.Method :?> TestObject
    let rec inner = function
    | EndMarker -> testResult.Success()
    | ContextResult ctx -> ctx.Children |> List.iter inner
    | TestResult tr ->
      match tr with
      | Error (meta, es, res) ->
        testResult.Error(List.head es)
      | Done (meta, res) ->
        match res |> AssertionResult.NonEmptyList.typicalResult with
        | Passed _ -> testResult.Success()
        | NotPassed (Skipped s) -> testResult.Ignore(s)
        | NotPassed (Violated s) -> testResult.Failure(s, "")
    TestRunner.runTests reporter test |> inner

  member private x.HandleException(ex, testResult, failureSite) =
    match ex with
    | :? ThreadAbortException -> Thread.ResetAbort()
    | _ -> ()
    x.RecordException(ex, testResult, failureSite)
