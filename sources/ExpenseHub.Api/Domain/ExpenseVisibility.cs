using System;
using System.Linq.Expressions;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Escopo de leitura dos reembolsos por role, aplicado na consulta ao banco antes de materializar os dados.
/// </summary>
public static class ExpenseVisibility
{
    /// <summary>
    /// Monta o filtro dos reembolsos visíveis para o usuário. As roles acumulam: o resultado é a união dos escopos.
    /// </summary>
    /// <param name="viewer">Usuário autenticado e suas roles.</param>
    /// <returns>O predicado traduzível para SQL.</returns>
    public static Expression<Func<Expense, bool>> VisibleTo(ExpenseViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        string userId = viewer.UserId;
        bool isEmployee = viewer.IsEmployee;
        bool isApprover = viewer.IsApprover;
        bool isFinance = viewer.IsFinance;
        bool isAuditor = viewer.IsAuditor;

        return expense =>
            isAuditor
            || (isEmployee && expense.OwnerId == userId)
            || (isApprover && expense.Status == ExpenseStatus.Submitted)
            || (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }

    /// <summary>
    /// Monta o filtro dos reembolsos que, mesmo fora do escopo de leitura, podem ser alvo da operação: os que já
    /// saíram de Draft, para quem tem a role da operação. Assim, aprovar de novo um reembolso já decidido responde
    /// 409 (transição incompatível) em vez de 404, e rascunhos de outras pessoas continuam invisíveis.
    /// </summary>
    /// <param name="viewer">Usuário autenticado e suas roles.</param>
    /// <param name="operation">Aprovar, reprovar ou pagar.</param>
    /// <returns>O predicado traduzível para SQL.</returns>
    public static Expression<Func<Expense, bool>> ActionableBy(ExpenseViewer viewer, ExpenseOperation operation)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        bool hasOperationRole = operation switch
        {
            ExpenseOperation.Approve or ExpenseOperation.Reject => viewer.IsApprover,
            ExpenseOperation.Pay => viewer.IsFinance,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Somente aprovar, reprovar e pagar."),
        };

        return expense => hasOperationRole && expense.Status != ExpenseStatus.Draft;
    }
}

/// <summary>
/// Usuário autenticado e as roles que dão acesso funcional aos reembolsos.
/// </summary>
/// <param name="UserId">Identificador do usuário, vindo do token.</param>
/// <param name="IsEmployee">Possui a role Employee.</param>
/// <param name="IsApprover">Possui a role Approver.</param>
/// <param name="IsFinance">Possui a role Finance.</param>
/// <param name="IsAuditor">Possui a role Auditor.</param>
public sealed record ExpenseViewer(string UserId, bool IsEmployee, bool IsApprover, bool IsFinance, bool IsAuditor);
