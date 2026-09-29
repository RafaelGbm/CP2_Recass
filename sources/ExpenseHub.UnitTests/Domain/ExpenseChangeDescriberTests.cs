using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Descrição das alterações gravada no histórico de edição do rascunho.
/// </summary>
[TestClass]
public sealed class ExpenseChangeDescriberTests
{
    private static readonly ExpenseDraftValues _original =
        new("Táxi do aeroporto ao hotel", 85.50m, new DateOnly(2026, 9, 20), 1);

    /// <summary>
    /// Sem diferença entre os valores, não há alteração a registrar.
    /// </summary>
    [TestMethod]
    public void Describe_SameValues_ReturnsNull()
    {
        ExpenseDraftValues same = _original with { Amount = 85.5000m };

        Assert.IsNull(ExpenseChangeDescriber.Describe(_original, same));
    }

    /// <summary>
    /// Só os campos alterados aparecem, com valor anterior e novo.
    /// </summary>
    [TestMethod]
    public void Describe_ChangedAmount_ListsOnlyAmount()
    {
        string? changes = ExpenseChangeDescriber.Describe(_original, _original with { Amount = 90m });

        Assert.AreEqual("amount: 85.50 -> 90.00", changes);
    }

    /// <summary>
    /// Várias alterações são listadas na ordem dos campos, em cultura invariante.
    /// </summary>
    [TestMethod]
    public void Describe_SeveralChanges_ListsAllInFieldOrder()
    {
        ExpenseDraftValues edited = new("Táxi do aeroporto à reunião", 1234.5m, new DateOnly(2026, 9, 21), null);

        string? changes = ExpenseChangeDescriber.Describe(_original, edited);

        Assert.AreEqual(
            "description: \"Táxi do aeroporto ao hotel\" -> \"Táxi do aeroporto à reunião\"; "
            + "amount: 85.50 -> 1234.50; expenseDate: 2026-09-20 -> 2026-09-21; categoryId: 1 -> null",
            changes);
    }

    /// <summary>
    /// A maior alteração possível (duas descrições de 500 caracteres e todos os campos) cabe no limite da coluna.
    /// </summary>
    [TestMethod]
    public void Describe_LargestChange_FitsHistoryColumn()
    {
        ExpenseDraftValues before = new(new string('a', 500), 0.01m, DateOnly.MinValue, null);
        ExpenseDraftValues after = new(new string('b', 500), 2147483647m, DateOnly.MaxValue, int.MaxValue);

        string? changes = ExpenseChangeDescriber.Describe(before, after);

        Assert.IsNotNull(changes);
        Assert.IsLessThanOrEqualTo(ExpenseHistory.ChangesMaxLength, changes.Length);
    }
}
