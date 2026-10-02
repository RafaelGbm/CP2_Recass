using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Matriz de acesso das operações de escrita: role, propriedade e estado decididos na camada de serviço.
/// </summary>
[TestClass]
public sealed class ExpenseAccessPolicyTests
{
    private const string Viewer = "viewer";
    private const string Other = "other";

    /// <summary>
    /// O Employee dono edita e envia o próprio rascunho.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit)]
    [DataRow(ExpenseOperation.Submit)]
    public void Decide_EmployeeOwnerOnDraft_IsAllowed(ExpenseOperation operation)
    {
        Assert.AreEqual(ExpenseAccessDecision.Allowed, Decide(Employee, operation, Viewer, ExpenseStatus.Draft));
    }

    /// <summary>
    /// Um Employee não edita nem envia o reembolso de outra pessoa, mesmo acumulando outras roles que o tornam visível.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit)]
    [DataRow(ExpenseOperation.Submit)]
    public void Decide_EmployeeOnSomeoneElsesExpense_IsForbidden(ExpenseOperation operation)
    {
        ExpenseViewer allRoles = new(Viewer, true, true, true, true);

        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(allRoles, operation, Other, ExpenseStatus.Draft));
    }

    /// <summary>
    /// Fora de Draft, o dono não edita nem reenvia: estado incompatível.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit, ExpenseStatus.Submitted)]
    [DataRow(ExpenseOperation.Edit, ExpenseStatus.Paid)]
    [DataRow(ExpenseOperation.Submit, ExpenseStatus.Submitted)]
    [DataRow(ExpenseOperation.Submit, ExpenseStatus.Rejected)]
    public void Decide_OwnerOutsideDraft_IsConflict(ExpenseOperation operation, ExpenseStatus status)
    {
        Assert.AreEqual(ExpenseAccessDecision.Conflict, Decide(Employee, operation, Viewer, status));
    }

    /// <summary>
    /// O Approver decide sobre o reembolso enviado de outra pessoa.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Reject)]
    public void Decide_ApproverOnSomeoneElsesSubmitted_IsAllowed(ExpenseOperation operation)
    {
        Assert.AreEqual(ExpenseAccessDecision.Allowed, Decide(Approver, operation, Other, ExpenseStatus.Submitted));
    }

    /// <summary>
    /// Ninguém aprova ou reprova o próprio reembolso, mesmo sendo Employee e Approver.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Reject)]
    public void Decide_ApproverOnOwnExpense_IsForbidden(ExpenseOperation operation)
    {
        ExpenseViewer employeeAndApprover = new(Viewer, true, true, false, false);

        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(employeeAndApprover, operation, Viewer, ExpenseStatus.Submitted));
    }

    /// <summary>
    /// O Approver só decide sobre reembolsos em Submitted; repetir a decisão é conflito.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Approve, ExpenseStatus.Draft)]
    [DataRow(ExpenseOperation.Approve, ExpenseStatus.Approved)]
    [DataRow(ExpenseOperation.Reject, ExpenseStatus.Rejected)]
    [DataRow(ExpenseOperation.Reject, ExpenseStatus.Paid)]
    public void Decide_ApproverOutsideSubmitted_IsConflict(ExpenseOperation operation, ExpenseStatus status)
    {
        Assert.AreEqual(ExpenseAccessDecision.Conflict, Decide(Approver, operation, Other, status));
    }

    /// <summary>
    /// O Finance paga o reembolso aprovado de outra pessoa.
    /// </summary>
    [TestMethod]
    public void Decide_FinanceOnSomeoneElsesApproved_IsAllowed()
    {
        Assert.AreEqual(ExpenseAccessDecision.Allowed, Decide(Finance, ExpenseOperation.Pay, Other, ExpenseStatus.Approved));
    }

    /// <summary>
    /// Ninguém paga o próprio reembolso, mesmo sendo Employee e Finance.
    /// </summary>
    [TestMethod]
    public void Decide_FinanceOnOwnExpense_IsForbidden()
    {
        ExpenseViewer employeeAndFinance = new(Viewer, true, false, true, false);

        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(employeeAndFinance, ExpenseOperation.Pay, Viewer, ExpenseStatus.Approved));
    }

    /// <summary>
    /// O Finance só paga reembolsos aprovados; pagar de novo é conflito.
    /// </summary>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow(ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Paid)]
    public void Decide_FinanceOutsideApproved_IsConflict(ExpenseStatus status)
    {
        Assert.AreEqual(ExpenseAccessDecision.Conflict, Decide(Finance, ExpenseOperation.Pay, Other, status));
    }

    /// <summary>
    /// Cada operação exige a própria role: outra role não a substitui.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    /// <param name="status">Estado em que a operação seria válida.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit, ExpenseStatus.Draft)]
    [DataRow(ExpenseOperation.Submit, ExpenseStatus.Draft)]
    [DataRow(ExpenseOperation.Approve, ExpenseStatus.Submitted)]
    [DataRow(ExpenseOperation.Pay, ExpenseStatus.Approved)]
    public void Decide_WithoutRequiredRole_IsForbidden(ExpenseOperation operation, ExpenseStatus status)
    {
        ExpenseViewer withoutRole = operation switch
        {
            ExpenseOperation.Edit or ExpenseOperation.Submit => new(Viewer, false, true, true, true),
            ExpenseOperation.Approve => new(Viewer, true, false, true, true),
            _ => new(Viewer, true, true, false, true),
        };
        string owner = operation is ExpenseOperation.Edit or ExpenseOperation.Submit ? Viewer : Other;

        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(withoutRole, operation, owner, status));
    }

    /// <summary>
    /// O Auditor consulta tudo, mas não altera dados.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit)]
    [DataRow(ExpenseOperation.Submit)]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Reject)]
    [DataRow(ExpenseOperation.Pay)]
    public void Decide_Auditor_CannotWrite(ExpenseOperation operation)
    {
        ExpenseViewer auditor = new(Viewer, false, false, false, true);

        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(auditor, operation, Other, status));
            Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(auditor, operation, Viewer, status));
        }
    }

    /// <summary>
    /// Sem role funcional (só Admin ou sem role), nenhuma operação é permitida.
    /// </summary>
    /// <param name="operation">Operação executada.</param>
    [TestMethod]
    [DataRow(ExpenseOperation.Edit)]
    [DataRow(ExpenseOperation.Submit)]
    [DataRow(ExpenseOperation.Approve)]
    [DataRow(ExpenseOperation.Pay)]
    public void Decide_WithoutFunctionalRole_IsForbidden(ExpenseOperation operation)
    {
        ExpenseViewer adminOnly = new(Viewer, false, false, false, false);

        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(adminOnly, operation, Viewer, ExpenseStatus.Draft));
    }

    /// <summary>
    /// A propriedade é verificada antes do estado: o não dono recebe 403 mesmo com o estado incompatível.
    /// </summary>
    [TestMethod]
    public void Decide_OwnershipIsCheckedBeforeStatus()
    {
        Assert.AreEqual(ExpenseAccessDecision.Forbidden, Decide(Employee, ExpenseOperation.Submit, Other, ExpenseStatus.Paid));
    }

    /// <summary>
    /// A edição não é uma transição de estado.
    /// </summary>
    [TestMethod]
    public void ToTransition_Edit_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ExpenseAccessPolicy.ToTransition(ExpenseOperation.Edit));
    }

    private static ExpenseViewer Employee => new(Viewer, true, false, false, false);

    private static ExpenseViewer Approver => new(Viewer, false, true, false, false);

    private static ExpenseViewer Finance => new(Viewer, false, false, true, false);

    private static ExpenseAccessDecision Decide(ExpenseViewer viewer, ExpenseOperation operation, string ownerId, ExpenseStatus status) =>
        ExpenseAccessPolicy.Decide(viewer, operation, ownerId, status);
}
