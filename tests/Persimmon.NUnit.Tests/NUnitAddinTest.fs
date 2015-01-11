namespace Persimmon.NUnit.Tests

open NUnit.Framework
open Persimmon
open Persimmon.NUnit

[<TestFixture>]
module NUnitAddinTest =

  let ``this test always succeed`` () = test "this test always succeed" {
    return 0
  }

  let ``let binding test`` = test "let binding test" {
    return 1
  }
