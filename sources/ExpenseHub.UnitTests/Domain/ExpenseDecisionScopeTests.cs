using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Reembolsos que aprovar, reprovar e pagar alcançam além do escopo de leitura, para que uma decisão repetida
/// responda 409 em vez de 404 sem expor rascunhos de outras pessoas.
/// </summary>
[TestClass]
public sealed class ExpenseDecisionScopeTests
{
    private const string Bob = "bob";

    private static readonly List<Expense> _expenses =
    [
        NewExpense("bob-draft", ExpenseStatus.Draft),
        NewExpense("bob-submitted", ExpenseStatus.Submitted),
        NewExpense("bob-approved", ExpenseStatus.Approved),
        NewExpense("bob-rejected", ExpenseStatus.Rejected),
        NewExpense("bob-paid", ExpenseStatus.Paid),
    ];

    /// <summary>
    /// O Approver alcança todo reembolso que já saiu de Draft, inclusive os já decididos, mas nunca um rascunho.
    /// </summary>
    /// <param name="operation">Aprovar ou reprovar.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Reject)]
    public void ActionableBy_ApproverDecision_ReachesEveryExpenseOutOfDraft(ExpenseOperation operation)
    {
        AssertActionable(
            new ExpenseViewer("carla", false, true, false, false),
            operation,
            "bob-submitted",
            "bob-approved",
            "bob-rejected",
            "bob-paid");
    }

    /// <summary>
    /// O Finance alcança todo reembolso que já saiu de Draft para pagar, mas nunca um rascunho.
    /// </summary>
    [TestMethod]
    public void ActionableBy_FinancePayment_ReachesEveryExpenseOutOfDraft()
    {
        AssertActionable(
            new ExpenseViewer("davi", false, false, true, false),
            ExpenseOperation.Pay,
            "bob-submitted",
            "bob-approved",
            "bob-rejected",
            "bob-paid");
    }

    /// <summary>
    /// Sem a role da operação, nada é alcançado: Finance não decide e Approver não paga.
    /// </summary>
    /// <param name="operation">Operação solicitada.</param>
    /// <param name="isApprover">Possui a role Approver.</param>
    /// <param name="isFinance">Possui a role Finance.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve, false, true)]
    [DataRow(ExpenseOperation.Reject, false, true)]
    [DataRow(ExpenseOperation.Pay, true, false)]
    public void ActionableBy_WithoutOperationRole_ReachesNothing(ExpenseOperation operation, bool isApprover, bool isFinance)
    {
        AssertActionable(new ExpenseViewer("eva", true, isApprover, isFinance, true), operation);
    }

    /// <summary>
    /// Editar e enviar continuam restritos ao escopo de leitura e não usam este filtro.
    /// </summary>
    /// <param name="operation">Operação que não é decisão nem pagamento.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit)]
    [DataRow(ExpenseOperation.Submit)]
    public void ActionableBy_EditOrSubmit_IsNotSupported(ExpenseOperation operation)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ExpenseVisibility.ActionableBy(new ExpenseViewer("carla", true, true, true, true), operation));
    }

    private static Expense NewExpense(string description, ExpenseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = Bob,
        Description = description,
        Amount = 10m,
        ExpenseDate = new DateOnly(2026, 9, 1),
        Status = status,
    };

    private static void AssertActionable(ExpenseViewer viewer, ExpenseOperation operation, params string[] expected)
    {
        Func<Expense, bool> predicate = ExpenseVisibility.ActionableBy(viewer, operation).Compile();
        string[] actual = [.. _expenses.Where(predicate).Select(expense => expense.Description)];
        CollectionAssert.AreEquivalent(expected, actual);
    }
}
