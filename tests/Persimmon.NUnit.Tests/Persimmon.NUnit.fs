namespace Persimmon.NUnit

open NUnit.Core.Extensibility

[<NUnitAddin(Description = "Persimmon addin")>]
type PersimmonAddin() =
  interface IAddin with
    override x.Install(host) =
      let builder = PersimmonTestCaseBuider()
      host.GetExtensionPoint("TestCaseBuilders").Install(builder)
      true
