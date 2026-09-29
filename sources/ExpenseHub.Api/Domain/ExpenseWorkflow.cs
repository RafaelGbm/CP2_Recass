using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Máquina de estados do reembolso, sem acessar o banco. É a única fonte das transições permitidas.
/// </summary>
public static class ExpenseWorkflow
{
    /// <summary>
    /// Indica se o reembolso ainda pode ser editado pelo dono.
    /// </summary>
    /// <param name="status">Estado atual.</param>
    /// <returns><see langword="true"/> somente em <see cref="ExpenseStatus.Draft"/>.</returns>
    public static bool CanEdit(ExpenseStatus status) => status == ExpenseStatus.Draft;

    /// <summary>
    /// Indica se o estado é final, sem nenhuma transição possível.
    /// </summary>
    /// <param name="status">Estado avaliado.</param>
    /// <returns><see langword="true"/> para <see cref="ExpenseStatus.Rejected"/> e <see cref="ExpenseStatus.Paid"/>.</returns>
    public static bool IsFinal(ExpenseStatus status) => status is ExpenseStatus.Rejected or ExpenseStatus.Paid;

    /// <summary>
    /// Calcula o próximo estado de uma transição a partir do estado atual.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="transition">Transição solicitada.</param>
    /// <param name="next">Próximo estado; igual ao atual quando a transição não é permitida.</param>
    /// <returns><see langword="true"/> quando o estado atual é o esperado pela transição.</returns>
    public static bool TryTransition(ExpenseStatus current, ExpenseTransition transition, out ExpenseStatus next)
    {
        (ExpenseStatus from, ExpenseStatus to) = Rule(transition);
        bool allowed = current == from;
        next = allowed ? to : current;
        return allowed;
    }

    /// <summary>
    /// Verifica a regra de propriedade da transição: só o dono envia, e o dono nunca aprova, reprova ou paga.
    /// </summary>
    /// <param name="transition">Transição solicitada.</param>
    /// <param name="isOwner">Indica se o ator é o dono do reembolso.</param>
    /// <returns><see langword="true"/> quando o ator pode executar a transição.</returns>
    public static bool SatisfiesOwnership(ExpenseTransition transition, bool isOwner) =>
        transition == ExpenseTransition.Submit ? isOwner : !isOwner;

    /// <summary>
    /// Nome da ação gravada no histórico para a transição.
    /// </summary>
    /// <param name="transition">Transição executada.</param>
    /// <returns>Um dos valores de <see cref="ExpenseActions"/>.</returns>
    public static string ActionName(ExpenseTransition transition) => transition switch
    {
        ExpenseTransition.Submit => ExpenseActions.Submitted,
        ExpenseTransition.Approve => ExpenseActions.Approved,
        ExpenseTransition.Reject => ExpenseActions.Rejected,
        ExpenseTransition.Pay => ExpenseActions.Paid,
        _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, "Transição desconhecida."),
    };

    private static (ExpenseStatus From, ExpenseStatus To) Rule(ExpenseTransition transition) => transition switch
    {
        ExpenseTransition.Submit => (ExpenseStatus.Draft, ExpenseStatus.Submitted),
        ExpenseTransition.Approve => (ExpenseStatus.Submitted, ExpenseStatus.Approved),
        ExpenseTransition.Reject => (ExpenseStatus.Submitted, ExpenseStatus.Rejected),
        ExpenseTransition.Pay => (ExpenseStatus.Approved, ExpenseStatus.Paid),
        _ => throw new ArgumentOutOfRangeException(nameof(transition), transition, "Transição desconhecida."),
    };
}

/// <summary>
/// Transições de estado disponíveis pelas rotas de ação.
/// </summary>
public enum ExpenseTransition
{
    /// <summary>Draft para Submitted, pelo dono.</summary>
    Submit = 0,

    /// <summary>Submitted para Approved, por um Approver que não é o dono.</summary>
    Approve = 1,

    /// <summary>Submitted para Rejected, por um Approver que não é o dono.</summary>
    Reject = 2,

    /// <summary>Approved para Paid, por um Finance que não é o dono.</summary>
    Pay = 3,
}
