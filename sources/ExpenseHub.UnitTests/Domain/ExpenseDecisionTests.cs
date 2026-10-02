using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Sequência de decisões sobre um reembolso enviado: ele recebe uma única decisão, e qualquer outra depois
/// dela é conflito.
/// </summary>
[TestClass]
public sealed class ExpenseDecisionTests
{
    private const string Owner = "ana";

    private static readonly ExpenseViewer _approver = new("carla", false, true, false, false);

    /// <summary>
    /// Depois da primeira decisão, aprovar ou reprovar de novo é conflito, seja qual for a decisão anterior.
    /// </summary>
    /// <param name="first">Primeira decisão, que é aceita.</param>
    /// <param name="second">Decisão seguinte, que é recusada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve, ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Approve, ExpenseOperation.Reject)]
    [DataRow(ExpenseOperation.Reject, ExpenseOperation.Reject)]
    [DataRow(ExpenseOperation.Reject, ExpenseOperation.Approve)]
    public void Decide_SecondDecision_IsConflict(ExpenseOperation first, ExpenseOperation second)
    {
        ExpenseStatus afterFirst = Apply(ExpenseStatus.Submitted, first);

        Assert.AreEqual(ExpenseAccessDecision.Conflict, ExpenseAccessPolicy.Decide(_approver, second, Owner, afterFirst));
    }

    /// <summary>
    /// Aprovar leva a Approved e reprovar leva a Rejected, que é final.
    /// </summary>
    /// <param name="decision">Decisão aplicada.</param>
    /// <param name="expected">Estado esperado depois dela.</param>
    /// <param name="expectedFinal">Se o estado resultante é final.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve, ExpenseStatus.Approved, false)]
    [DataRow(ExpenseOperation.Reject, ExpenseStatus.Rejected, true)]
    public void Decide_OnSubmitted_MovesToDecisionStatus(ExpenseOperation decision, ExpenseStatus expected, bool expectedFinal)
    {
        ExpenseStatus result = Apply(ExpenseStatus.Submitted, decision);

        Assert.AreEqual(expected, result);
        Assert.AreEqual(expectedFinal, ExpenseWorkflow.IsFinal(result));
    }

    /// <summary>
    /// Um usuário com Employee, Approver, Finance e Auditor continua proibido de decidir o próprio reembolso.
    /// </summary>
    /// <param name="decision">Aprovar ou reprovar.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Reject)]
    public void Decide_OwnerWithEveryRole_IsForbidden(ExpenseOperation decision)
    {
        ExpenseViewer owner = new(Owner, true, true, true, true);

        Assert.AreEqual(
            ExpenseAccessDecision.Forbidden,
            ExpenseAccessPolicy.Decide(owner, decision, Owner, ExpenseStatus.Submitted));
    }

    /// <summary>
    /// A ação gravada no histórico corresponde à decisão tomada.
    /// </summary>
    /// <param name="decision">Aprovar ou reprovar.</param>
    /// <param name="expectedAction">Ação esperada no histórico.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve, ExpenseActions.Approved)]
    [DataRow(ExpenseOperation.Reject, ExpenseActions.Rejected)]
    public void ActionName_ForDecision_MatchesHistoryAction(ExpenseOperation decision, string expectedAction)
    {
        Assert.AreEqual(expectedAction, ExpenseWorkflow.ActionName(ExpenseAccessPolicy.ToTransition(decision)));
    }

    private static ExpenseStatus Apply(ExpenseStatus current, ExpenseOperation decision)
    {
        Assert.AreEqual(ExpenseAccessDecision.Allowed, ExpenseAccessPolicy.Decide(_approver, decision, Owner, current));
        Assert.IsTrue(ExpenseWorkflow.TryTransition(current, ExpenseAccessPolicy.ToTransition(decision), out ExpenseStatus next));
        return next;
    }
}
