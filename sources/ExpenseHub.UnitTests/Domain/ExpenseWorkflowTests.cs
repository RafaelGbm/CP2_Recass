using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Transições de estado e regras de propriedade do reembolso.
/// </summary>
[TestClass]
public sealed class ExpenseWorkflowTests
{
    /// <summary>
    /// Cada transição leva do estado esperado ao próximo estado do contrato.
    /// </summary>
    /// <param name="transition">Transição executada.</param>
    /// <param name="from">Estado esperado pela transição.</param>
    /// <param name="to">Próximo estado.</param>
    [TestMethod]
    [DataRow(ExpenseTransition.Submit, ExpenseStatus.Draft, ExpenseStatus.Submitted)]
    [DataRow(ExpenseTransition.Approve, ExpenseStatus.Submitted, ExpenseStatus.Approved)]
    [DataRow(ExpenseTransition.Reject, ExpenseStatus.Submitted, ExpenseStatus.Rejected)]
    [DataRow(ExpenseTransition.Pay, ExpenseStatus.Approved, ExpenseStatus.Paid)]
    public void TryTransition_FromExpectedStatus_MovesToNextStatus(
        ExpenseTransition transition,
        ExpenseStatus from,
        ExpenseStatus to)
    {
        bool allowed = ExpenseWorkflow.TryTransition(from, transition, out ExpenseStatus next);

        Assert.IsTrue(allowed);
        Assert.AreEqual(to, next);
    }

    /// <summary>
    /// Transição fora do estado esperado é recusada e mantém o estado (a rota responde 409).
    /// </summary>
    /// <param name="transition">Transição executada.</param>
    /// <param name="current">Estado atual incompatível.</param>
    [TestMethod]
    [DataRow(ExpenseTransition.Submit, ExpenseStatus.Submitted)]
    [DataRow(ExpenseTransition.Approve, ExpenseStatus.Draft)]
    [DataRow(ExpenseTransition.Reject, ExpenseStatus.Approved)]
    [DataRow(ExpenseTransition.Pay, ExpenseStatus.Submitted)]
    [DataRow(ExpenseTransition.Pay, ExpenseStatus.Draft)]
    public void TryTransition_FromUnexpectedStatus_IsRejected(ExpenseTransition transition, ExpenseStatus current)
    {
        bool allowed = ExpenseWorkflow.TryTransition(current, transition, out ExpenseStatus next);

        Assert.IsFalse(allowed);
        Assert.AreEqual(current, next);
    }

    /// <summary>
    /// Repetir uma transição já aplicada é recusado, o que evita histórico duplicado.
    /// </summary>
    [TestMethod]
    public void TryTransition_RepeatedSubmit_IsRejected()
    {
        ExpenseWorkflow.TryTransition(ExpenseStatus.Draft, ExpenseTransition.Submit, out ExpenseStatus submitted);

        bool allowedAgain = ExpenseWorkflow.TryTransition(submitted, ExpenseTransition.Submit, out _);

        Assert.IsFalse(allowedAgain);
    }

    /// <summary>
    /// Rejected e Paid são finais: nenhuma transição sai deles.
    /// </summary>
    /// <param name="final">Estado final.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void TryTransition_FromFinalStatus_IsAlwaysRejected(ExpenseStatus final)
    {
        Assert.IsTrue(ExpenseWorkflow.IsFinal(final));

        foreach (ExpenseTransition transition in Enum.GetValues<ExpenseTransition>())
        {
            Assert.IsFalse(ExpenseWorkflow.TryTransition(final, transition, out _));
        }
    }

    /// <summary>
    /// Só o rascunho pode ser editado.
    /// </summary>
    /// <param name="status">Estado avaliado.</param>
    /// <param name="expected">Se a edição é permitida.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Draft, true)]
    [DataRow(ExpenseStatus.Submitted, false)]
    [DataRow(ExpenseStatus.Approved, false)]
    [DataRow(ExpenseStatus.Rejected, false)]
    [DataRow(ExpenseStatus.Paid, false)]
    public void CanEdit_OnlyDraftIsEditable(ExpenseStatus status, bool expected)
    {
        Assert.AreEqual(expected, ExpenseWorkflow.CanEdit(status));
    }

    /// <summary>
    /// Só o dono envia; ninguém aprova, reprova ou paga o próprio reembolso.
    /// </summary>
    /// <param name="transition">Transição executada.</param>
    /// <param name="isOwner">Se o ator é o dono.</param>
    /// <param name="expected">Se a transição é permitida.</param>
    [TestMethod]
    [DataRow(ExpenseTransition.Submit, true, true)]
    [DataRow(ExpenseTransition.Submit, false, false)]
    [DataRow(ExpenseTransition.Approve, true, false)]
    [DataRow(ExpenseTransition.Approve, false, true)]
    [DataRow(ExpenseTransition.Reject, true, false)]
    [DataRow(ExpenseTransition.Pay, true, false)]
    [DataRow(ExpenseTransition.Pay, false, true)]
    public void SatisfiesOwnership_AppliesOwnerRules(ExpenseTransition transition, bool isOwner, bool expected)
    {
        Assert.AreEqual(expected, ExpenseWorkflow.SatisfiesOwnership(transition, isOwner));
    }

    /// <summary>
    /// Cada transição grava no histórico o nome de ação combinado.
    /// </summary>
    /// <param name="transition">Transição executada.</param>
    /// <param name="action">Ação esperada no histórico.</param>
    [TestMethod]
    [DataRow(ExpenseTransition.Submit, ExpenseActions.Submitted)]
    [DataRow(ExpenseTransition.Approve, ExpenseActions.Approved)]
    [DataRow(ExpenseTransition.Reject, ExpenseActions.Rejected)]
    [DataRow(ExpenseTransition.Pay, ExpenseActions.Paid)]
    public void ActionName_MapsTransitionToHistoryAction(ExpenseTransition transition, string action)
    {
        Assert.AreEqual(action, ExpenseWorkflow.ActionName(transition));
    }
}
