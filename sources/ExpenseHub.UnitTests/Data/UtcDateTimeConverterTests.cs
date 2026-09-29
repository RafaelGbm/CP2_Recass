using System;
using ExpenseHub.Api.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Data;

/// <summary>
/// Os instantes gravados e lidos do banco ficam sempre em UTC.
/// </summary>
[TestClass]
public sealed class UtcDateTimeConverterTests
{
    private static readonly DateTime _instant = new(2026, 9, 29, 12, 30, 0);

    /// <summary>
    /// Um instante já em UTC é gravado sem alteração.
    /// </summary>
    [TestMethod]
    public void ToUtc_KeepsUtcValue()
    {
        DateTime utc = DateTime.SpecifyKind(_instant, DateTimeKind.Utc);

        DateTime stored = UtcDateTimeConverter.ToUtc(utc);

        Assert.AreEqual(utc, stored);
        Assert.AreEqual(DateTimeKind.Utc, stored.Kind);
    }

    /// <summary>
    /// Um instante local é convertido para UTC antes de ser gravado.
    /// </summary>
    [TestMethod]
    public void ToUtc_ConvertsLocalValue()
    {
        DateTime local = DateTime.SpecifyKind(_instant, DateTimeKind.Local);

        DateTime stored = UtcDateTimeConverter.ToUtc(local);

        Assert.AreEqual(local.ToUniversalTime(), stored);
        Assert.AreEqual(DateTimeKind.Utc, stored.Kind);
    }

    /// <summary>
    /// O valor lido do banco volta marcado como UTC, sem mudar o horário.
    /// </summary>
    [TestMethod]
    public void ConvertFromProvider_MarksValueAsUtc()
    {
        UtcDateTimeConverter converter = new();
        DateTime fromDatabase = DateTime.SpecifyKind(_instant, DateTimeKind.Unspecified);

        DateTime read = (DateTime)converter.ConvertFromProvider(fromDatabase)!;

        Assert.AreEqual(DateTimeKind.Utc, read.Kind);
        Assert.AreEqual(_instant.Ticks, read.Ticks);
    }
}
