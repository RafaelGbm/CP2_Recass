using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Calcula o efeito de uma transição sobre um reembolso, sem alterá-lo nem acessar o banco.
/// </summary>
public static class ExpenseTransitionPlanner
{
    /// <summary>
    /// Decide a transição e monta o novo estado, o histórico (com a justificativa, na reprovação) e, no pagamento, o registro de pagamento.
    /// </summary>
    /// <param name="expense">Reembolso visível para o usuário.</param>
    /// <param name="viewer">Usuário autenticado e suas roles.</param>
    /// <param name="operation">Operação de ação solicitada.</param>
    /// <param name="justification">Justificativa da reprovação, já validada; ignorada nas outras transições.</param>
    /// <param name="nowUtc">Instante da operação, definido pelo servidor.</param>
    /// <returns>O plano permitido ou a recusa, sem histórico nem pagamento.</returns>
    public static ExpenseTransitionPlan Plan(
        Expense expense,
        ExpenseViewer viewer,
        ExpenseOperation operation,
        string? justification,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(expense);
        ArgumentNullException.ThrowIfNull(viewer);

        ExpenseAccessDecision decision = ExpenseAccessPolicy.Decide(viewer, operation, expense.OwnerId, expense.Status);
        if (decision != ExpenseAccessDecision.Allowed)
        {
            return ExpenseTransitionPlan.Refused(decision, expense.Status);
        }

        ExpenseTransition transition = ExpenseAccessPolicy.ToTransition(operation);
        ExpenseWorkflow.TryTransition(expense.Status, transition, out ExpenseStatus next);

        ExpenseHistory history = new()
        {
            ExpenseId = expense.Id,
            Action = ExpenseWorkflow.ActionName(transition),
            ActorId = viewer.UserId,
            OccurredAtUtc = nowUtc,
            PreviousStatus = expense.Status,
            NewStatus = next,
            Justification = transition == ExpenseTransition.Reject ? justification : null,
        };

        PaymentRecord? payment = transition == ExpenseTransition.Pay
            ? new PaymentRecord { ExpenseId = expense.Id, PaidById = viewer.UserId, PaidAtUtc = nowUtc }
            : null;

        return new ExpenseTransitionPlan(ExpenseAccessDecision.Allowed, next, history, payment);
    }
}

/// <summary>
/// Resultado de <see cref="ExpenseTransitionPlanner.Plan"/>.
/// </summary>
/// <param name="Decision">Permitido, proibido (403) ou estado incompatível (409).</param>
/// <param name="NewStatus">Estado depois da transição; igual ao atual na recusa.</param>
/// <param name="History">Registro de histórico a gravar; nulo na recusa.</param>
/// <param name="Payment">Registro de pagamento a gravar; só existe no pagamento permitido.</param>
public sealed record ExpenseTransitionPlan(
    ExpenseAccessDecision Decision,
    ExpenseStatus NewStatus,
    ExpenseHistory? History,
    PaymentRecord? Payment)
{
    /// <summary>
    /// Cria um plano recusado, sem nada a gravar.
    /// </summary>
    /// <param name="decision">Motivo da recusa.</param>
    /// <param name="currentStatus">Estado atual, que não muda.</param>
    /// <returns>O plano recusado.</returns>
    public static ExpenseTransitionPlan Refused(ExpenseAccessDecision decision, ExpenseStatus currentStatus) =>
        new(decision, currentStatus, null, null);
}
