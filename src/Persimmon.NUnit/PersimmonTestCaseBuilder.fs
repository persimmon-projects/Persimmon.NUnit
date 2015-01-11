namespace Persimmon.NUnit

open NUnit.Core
open NUnit.Core.Extensibility

open System
open System.Reflection
open Persimmon

module private Impl =

  let (|SubTypeOf|_|) (matching: Type) (typ: Type) =
    if matching.IsAssignableFrom(typ) then Some typ else None

  let isPersimmonTests (f: unit -> obj) (typ: Type) =
    let testObjType = typeof<TestObject>
    match typ with
    | SubTypeOf testObjType _ -> true
    | _ -> false
  
  let isPersimmonTestMethods (m: MethodInfo) =
    isPersimmonTests (fun () -> m.Invoke(null, [||])) m.ReturnType

type PersimmonTestCaseBuider() =
  interface ITestCaseBuilder with
    override x.CanBuildFrom(m) =
      m.GetParameters() |> Array.isEmpty
      && Impl.isPersimmonTestMethods(m)
    override x.BuildFrom(m) =
      PersimmonTestMethod(m) :> Test
