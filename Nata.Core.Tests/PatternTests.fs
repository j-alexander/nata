namespace Nata.Core.Tests

open System
open NUnit.Framework
open Nata.Core

[<TestFixture>]
type PatternTests() =

    [<Test>]
    member x.TestDecimalPattern() =
        Assert.AreEqual(Some Decimal.MaxValue, (|Decimal|_|) <| Decimal.MaxValue.ToString())
        Assert.AreEqual(Some Decimal.MinValue, (|Decimal|_|) <| Decimal.MinValue.ToString())
        Assert.AreEqual(Some 0.0m, (|Decimal|_|) "0.0")
        Assert.AreEqual(Some 0m, (|Decimal|_|) "0")
        Assert.AreEqual(None, (|Decimal|_|) String.Empty)
        Assert.AreEqual(None, (|Decimal|_|) null)

    [<Test>]
    member x.TestInteger64Pattern() =
        Assert.AreEqual(Some Int64.MaxValue, (|Integer64|_|) <| Int64.MaxValue.ToString())
        Assert.AreEqual(Some Int64.MinValue, (|Integer64|_|) <| Int64.MinValue.ToString())
        Assert.AreEqual(Some 0L, (|Integer64|_|) "0")
        Assert.AreEqual(None, (|Integer64|_|) String.Empty)
        Assert.AreEqual(None, (|Integer64|_|) null)

    [<Test>]
    member x.TestInteger32Pattern() =
        Assert.AreEqual(Some Int32.MaxValue, (|Integer32|_|) <| Int32.MaxValue.ToString())
        Assert.AreEqual(Some Int32.MinValue, (|Integer32|_|) <| Int32.MinValue.ToString())
        Assert.AreEqual(Some 0, (|Integer32|_|) "0")
        Assert.AreEqual(None, (|Integer32|_|) String.Empty)
        Assert.AreEqual(None, (|Integer32|_|) null)
        Assert.AreEqual(None, (|Integer32|_|) <| Int64.MaxValue.ToString())
        Assert.AreEqual(None, (|Integer32|_|) <| Int64.MinValue.ToString())

    [<Test>]
    member x.TestDateTimePattern() =
        let date = DateTime.UtcNow
        Assert.AreEqual(Some (DateTime.Resolution.day date), (|DateTime|_|) <| date.ToLongDateString())
        Assert.AreEqual(Some (DateTime.Resolution.day date), (|DateTime|_|) <| date.ToShortDateString())
        Assert.AreEqual(Some (DateTime.Resolution.second date), (|DateTime|_|) <| date.ToString("yyyy/MM/dd HH:mm:ss"))
        Assert.AreEqual(None, (|DateTime|_|) <| String.Empty)
        Assert.AreEqual(None, (|DateTime|_|) <| null)