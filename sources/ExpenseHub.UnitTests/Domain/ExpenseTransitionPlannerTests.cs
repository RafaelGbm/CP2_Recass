using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Efeito das transições: novo estado, histórico e registro de pagamento, ou nada a gravar na recusa.
/// </summary>
[TestClass]
public sealed class ExpenseTransitionPlannerTests
{
    private const string Owner = "owner";
    private const string FinanceUser = "finance";

    private static readonly DateTime _now = new(2026, 10, 1, 13, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// Finance que não é o dono paga o aprovado: Approved para Paid, com histórico e pagamento do mesmo ator e instante.
    /// </summary>
    [TestMethod]
    public void Plan_FinancePaysApproved_MovesToPaidWithHistoryAndPayment()
    {
        Expense expense = NewExpense(ExpenseStatus.Approved);

        ExpenseTransitionPlan plan = ExpenseTransitionPlanner.Plan(expense, Finance(FinanceUser), ExpenseOperation.Pay, _now);

        Assert.AreEqual(ExpenseAccessDecision.Allowed, plan.Decision);
        Assert.AreEqual(ExpenseStatus.Paid, plan.NewStatus);

        Assert.IsNotNull(plan.History);
        Assert.AreEqual(ExpenseActions.Paid, plan.History.Action);
        Assert.AreEqual(expense.Id, plan.History.ExpenseId);
        Assert.AreEqual(FinanceUser, plan.History.ActorId);
        Assert.AreEqual(_now, plan.History.OccurredAtUtc);
        Assert.AreEqual(ExpenseStatus.Approved, plan.History.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Paid, plan.History.NewStatus);

        Assert.IsNotNull(plan.Payment);
        Assert.AreEqual(expense.Id, plan.Payment.ExpenseId);
        Assert.AreEqual(FinanceUser, plan.Payment.PaidById);
        Assert.AreEqual(_now, plan.Payment.PaidAtUtc);
    }

    /// <summary>
    /// Submitted, Rejected e Paid não são pagos; pagar de novo é conflito e nada é gravado.
    /// </summary>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void Plan_PayOutsideApproved_IsConflictWithNothingToSave(ExpenseStatus status)
    {
        ExpenseTransitionPlan plan = ExpenseTransitionPlanner.Plan(NewExpense(status), Finance(FinanceUser), ExpenseOperation.Pay, _now);

        AssertRefused(plan, ExpenseAccessDecision.Conflict, status);
    }

    /// <summary>
    /// Finance que é o dono não paga o próprio reembolso, mesmo sendo também Employee.
    /// </summary>
    [TestMethod]
    public void Plan_FinanceOwner_IsForbiddenWithNothingToSave()
    {
        ExpenseViewer employeeAndFinance = new(Owner, true, false, true, false);

        ExpenseTransitionPlan plan = ExpenseTransitionPlanner.Plan(NewExpense(ExpenseStatus.Approved), employeeAndFinance, ExpenseOperation.Pay, _now);

        AssertRefused(plan, ExpenseAccessDecision.Forbidden, ExpenseStatus.Approved);
    }

    /// <summary>
    /// O Auditor consulta, mas não paga.
    /// </summary>
    [TestMethod]
    public void Plan_AuditorPays_IsForbiddenWithNothingToSave()
    {
        ExpenseViewer auditor = new("auditor", false, false, false, true);

        ExpenseTransitionPlan plan = ExpenseTransitionPlanner.Plan(NewExpense(ExpenseStatus.Approved), auditor, ExpenseOperation.Pay, _now);

        AssertRefused(plan, ExpenseAccessDecision.Forbidden, ExpenseStatus.Approved);
    }

    /// <summary>
    /// Transições que não são pagamento geram histórico, mas nenhum registro de pagamento.
    /// </summary>
    [TestMethod]
    public void Plan_Submit_HasHistoryWithoutPayment()
    {
        ExpenseViewer owner = new(Owner, true, false, false, false);

        ExpenseTransitionPlan plan = ExpenseTransitionPlanner.Plan(NewExpense(ExpenseStatus.Draft), owner, ExpenseOperation.Submit, _now);

        Assert.AreEqual(ExpenseStatus.Submitted, plan.NewStatus);
        Assert.IsNotNull(plan.History);
        Assert.AreEqual(ExpenseActions.Submitted, plan.History.Action);
        Assert.AreEqual(ExpenseStatus.Draft, plan.History.PreviousStatus);
        Assert.IsNull(plan.Payment);
    }

    /// <summary>
    /// O planejamento não altera o reembolso: quem grava é o serviço, junto com o histórico.
    /// </summary>
    [TestMethod]
    public void Plan_DoesNotMutateExpense()
    {
        Expense expense = NewExpense(ExpenseStatus.Approved);

        ExpenseTransitionPlanner.Plan(expense, Finance(FinanceUser), ExpenseOperation.Pay, _now);

        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
    }

    private static ExpenseViewer Finance(string userId) => new(userId, false, false, true, false);

    private static Expense NewExpense(ExpenseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = Owner,
        Description = "Hospedagem em evento",
        Amount = 350m,
        ExpenseDate = new DateOnly(2026, 9, 20),
        Status = status,
    };

    private static void AssertRefused(ExpenseTransitionPlan plan, ExpenseAccessDecision decision, ExpenseStatus status)
    {
        Assert.AreEqual(decision, plan.Decision);
        Assert.AreEqual(status, plan.NewStatus);
        Assert.IsNull(plan.History);
        Assert.IsNull(plan.Payment);
    }
}
