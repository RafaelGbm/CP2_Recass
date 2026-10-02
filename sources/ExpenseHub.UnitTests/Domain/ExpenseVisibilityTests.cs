using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Escopo de leitura dos reembolsos por role, aplicado sobre uma lista em memória com o mesmo predicado usado no banco.
/// </summary>
[TestClass]
public sealed class ExpenseVisibilityTests
{
    private const string Ana = "ana";
    private const string Bob = "bob";

    private static readonly List<Expense> _expenses =
    [
        NewExpense("ana-draft", Ana, ExpenseStatus.Draft),
        NewExpense("ana-submitted", Ana, ExpenseStatus.Submitted),
        NewExpense("bob-draft", Bob, ExpenseStatus.Draft),
        NewExpense("bob-submitted", Bob, ExpenseStatus.Submitted),
        NewExpense("bob-approved", Bob, ExpenseStatus.Approved),
        NewExpense("bob-rejected", Bob, ExpenseStatus.Rejected),
        NewExpense("bob-paid", Bob, ExpenseStatus.Paid),
    ];

    /// <summary>
    /// Employee vê somente os próprios reembolsos, em qualquer estado, e nenhum de outro Employee.
    /// </summary>
    [TestMethod]
    public void VisibleTo_Employee_SeesOnlyOwnExpenses()
    {
        AssertVisible(new ExpenseViewer(Ana, true, false, false, false), "ana-draft", "ana-submitted");
    }

    /// <summary>
    /// Approver vê somente os enviados, de qualquer dono.
    /// </summary>
    [TestMethod]
    public void VisibleTo_Approver_SeesOnlySubmitted()
    {
        AssertVisible(new ExpenseViewer("carla", false, true, false, false), "ana-submitted", "bob-submitted");
    }

    /// <summary>
    /// Finance vê somente os aprovados e os pagos.
    /// </summary>
    [TestMethod]
    public void VisibleTo_Finance_SeesApprovedAndPaid()
    {
        AssertVisible(new ExpenseViewer("davi", false, false, true, false), "bob-approved", "bob-paid");
    }

    /// <summary>
    /// Auditor vê todos os reembolsos.
    /// </summary>
    [TestMethod]
    public void VisibleTo_Auditor_SeesAll()
    {
        List<string> visible = Visible(new ExpenseViewer("eva", false, false, false, true));

        Assert.HasCount(_expenses.Count, visible);
    }

    /// <summary>
    /// Roles acumulam: Employee e Approver vê os próprios e os enviados de outras pessoas.
    /// </summary>
    [TestMethod]
    public void VisibleTo_EmployeeAndApprover_SeesUnionOfScopes()
    {
        AssertVisible(new ExpenseViewer(Ana, true, true, false, false), "ana-draft", "ana-submitted", "bob-submitted");
    }

    /// <summary>
    /// Sem role funcional (usuário sem role ou só Admin), nada é visível.
    /// </summary>
    [TestMethod]
    public void VisibleTo_NoFunctionalRole_SeesNothing()
    {
        Assert.IsEmpty(Visible(new ExpenseViewer(Ana, false, false, false, false)));
    }

    private static Expense NewExpense(string description, string ownerId, ExpenseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Description = description,
        Amount = 10m,
        ExpenseDate = new DateOnly(2026, 9, 1),
        Status = status,
    };

    private static List<string> Visible(ExpenseViewer viewer)
    {
        Func<Expense, bool> predicate = ExpenseVisibility.VisibleTo(viewer).Compile();
        return [.. _expenses.Where(predicate).Select(expense => expense.Description)];
    }

    private static void AssertVisible(ExpenseViewer viewer, params string[] expected)
    {
        string[] actual = [.. Visible(viewer)];
        CollectionAssert.AreEquivalent(expected, actual);
    }
}
