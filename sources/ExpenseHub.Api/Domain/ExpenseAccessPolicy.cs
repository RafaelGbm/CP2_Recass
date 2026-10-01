using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Matriz de acesso das operações de escrita sobre um reembolso visível, combinando role, propriedade e estado, sem acessar o banco.
/// </summary>
public static class ExpenseAccessPolicy
{
    /// <summary>
    /// Decide se o usuário pode executar a operação. A ordem é role, depois propriedade, depois estado.
    /// </summary>
    /// <param name="viewer">Usuário autenticado e suas roles.</param>
    /// <param name="operation">Operação solicitada.</param>
    /// <param name="ownerId">Dono do reembolso.</param>
    /// <param name="status">Estado atual do reembolso.</param>
    /// <returns>A decisão: permitido, proibido (403) ou estado incompatível (409).</returns>
    public static ExpenseAccessDecision Decide(
        ExpenseViewer viewer,
        ExpenseOperation operation,
        string ownerId,
        ExpenseStatus status)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        if (!HasRequiredRole(viewer, operation))
        {
            return ExpenseAccessDecision.Forbidden;
        }

        bool isOwner = string.Equals(ownerId, viewer.UserId, StringComparison.Ordinal);
        if (!SatisfiesOwnership(operation, isOwner))
        {
            return ExpenseAccessDecision.Forbidden;
        }

        return IsAllowedInStatus(operation, status) ? ExpenseAccessDecision.Allowed : ExpenseAccessDecision.Conflict;
    }

    /// <summary>
    /// Transição de estado correspondente a uma operação de ação.
    /// </summary>
    /// <param name="operation">Operação de ação.</param>
    /// <returns>A transição executada pela operação.</returns>
    public static ExpenseTransition ToTransition(ExpenseOperation operation) => operation switch
    {
        ExpenseOperation.Submit => ExpenseTransition.Submit,
        ExpenseOperation.Approve => ExpenseTransition.Approve,
        ExpenseOperation.Reject => ExpenseTransition.Reject,
        ExpenseOperation.Pay => ExpenseTransition.Pay,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "A operação não é uma transição de estado."),
    };

    private static bool HasRequiredRole(ExpenseViewer viewer, ExpenseOperation operation) => operation switch
    {
        ExpenseOperation.Edit or ExpenseOperation.Submit => viewer.IsEmployee,
        ExpenseOperation.Approve or ExpenseOperation.Reject => viewer.IsApprover,
        ExpenseOperation.Pay => viewer.IsFinance,
        _ => false,
    };

    private static bool SatisfiesOwnership(ExpenseOperation operation, bool isOwner) =>
        operation == ExpenseOperation.Edit ? isOwner : ExpenseWorkflow.SatisfiesOwnership(ToTransition(operation), isOwner);

    private static bool IsAllowedInStatus(ExpenseOperation operation, ExpenseStatus status) =>
        operation == ExpenseOperation.Edit
            ? ExpenseWorkflow.CanEdit(status)
            : ExpenseWorkflow.TryTransition(status, ToTransition(operation), out _);
}

/// <summary>
/// Operações de escrita sobre um reembolso existente.
/// </summary>
public enum ExpenseOperation
{
    /// <summary>Editar o rascunho: Employee dono, em Draft.</summary>
    Edit = 0,

    /// <summary>Enviar: Employee dono, em Draft.</summary>
    Submit = 1,

    /// <summary>Aprovar: Approver que não é o dono, em Submitted.</summary>
    Approve = 2,

    /// <summary>Reprovar: Approver que não é o dono, em Submitted.</summary>
    Reject = 3,

    /// <summary>Pagar: Finance que não é o dono, em Approved.</summary>
    Pay = 4,
}

/// <summary>
/// Resultado de <see cref="ExpenseAccessPolicy.Decide"/>.
/// </summary>
public enum ExpenseAccessDecision
{
    /// <summary>A operação pode ser executada.</summary>
    Allowed = 0,

    /// <summary>Falta a role exigida ou a regra de propriedade proíbe a operação (403).</summary>
    Forbidden = 1,

    /// <summary>O estado atual não permite a operação (409).</summary>
    Conflict = 2,
}
