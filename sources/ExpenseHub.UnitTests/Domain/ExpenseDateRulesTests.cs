using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Regra de data não futura, com o dia de referência em Brasília.
/// </summary>
[TestClass]
public sealed class ExpenseDateRulesTests
{
    /// <summary>
    /// Às 22h de Brasília já é o dia seguinte em UTC, mas o dia de referência continua sendo o de Brasília.
    /// </summary>
    [TestMethod]
    public void TodayInBrazil_LateEveningInBrazil_UsesBrazilianDate()
    {
        DateTimeOffset utcNow = new(2026, 9, 30, 1, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(new DateOnly(2026, 9, 29), ExpenseDateRules.TodayInBrazil(utcNow));
    }

    /// <summary>
    /// Durante o dia, a data em Brasília coincide com a data UTC.
    /// </summary>
    [TestMethod]
    public void TodayInBrazil_Afternoon_MatchesUtcDate()
    {
        DateTimeOffset utcNow = new(2026, 9, 29, 15, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(new DateOnly(2026, 9, 29), ExpenseDateRules.TodayInBrazil(utcNow));
    }

    /// <summary>
    /// Hoje e datas passadas são aceitas; amanhã é recusado.
    /// </summary>
    /// <param name="daysFromToday">Distância em dias do dia de referência.</param>
    /// <param name="expected">Se a data é aceita.</param>
    [TestMethod]
    [DataRow(-30, true)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    public void IsNotFuture_ComparesWithReferenceDay(int daysFromToday, bool expected)
    {
        DateOnly today = new(2026, 9, 29);

        Assert.AreEqual(expected, ExpenseDateRules.IsNotFuture(today.AddDays(daysFromToday), today));
    }
}
