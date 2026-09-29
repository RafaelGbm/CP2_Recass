using System.Collections.Generic;
using ExpenseHub.Api.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Auth;

/// <summary>
/// Regras de alteração de roles aplicadas pelo Admin.
/// </summary>
[TestClass]
public sealed class RoleChangePlannerTests
{
    /// <summary>
    /// O conjunto solicitado substitui o atual: adiciona o que falta e remove o que sobrou.
    /// </summary>
    [TestMethod]
    public void Plan_ReplacesCurrentRolesWithRequestedSet()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Employee], [Roles.Approver, Roles.Finance], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        AssertRoles(plan.ToAdd, Roles.Approver, Roles.Finance);
        AssertRoles(plan.ToRemove, Roles.Employee);
        AssertRoles(plan.FinalRoles, Roles.Approver, Roles.Finance);
    }

    /// <summary>
    /// Roles fora da lista conhecida são recusadas e nada é alterado.
    /// </summary>
    [TestMethod]
    public void Plan_RejectsUnknownRoleWithoutChanges()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Employee], [Roles.Employee, "SuperUser"], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.UnknownRoles, plan.Status);
        AssertRoles(plan.UnknownRoles, "SuperUser");
        Assert.IsEmpty(plan.ToAdd);
        Assert.IsEmpty(plan.ToRemove);
    }

    /// <summary>
    /// Nome de role vazio ou nulo não cria role implicitamente.
    /// </summary>
    [TestMethod]
    public void Plan_RejectsBlankRoleName()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([], [" ", null!], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.UnknownRoles, plan.Status);
        Assert.HasCount(2, plan.UnknownRoles);
    }

    /// <summary>
    /// O Admin não pode remover a própria role Admin.
    /// </summary>
    [TestMethod]
    public void Plan_RejectsAdminRemovingOwnAdminRole()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Admin], [Roles.Employee], isSelf: true);

        Assert.AreEqual(RoleChangeStatus.SelfAdminRemoval, plan.Status);
        Assert.IsEmpty(plan.ToRemove);
    }

    /// <summary>
    /// A proteção vale só para a própria conta: o Admin pode tirar Admin de outro usuário.
    /// </summary>
    [TestMethod]
    public void Plan_AllowsRemovingAdminFromAnotherUser()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Admin], [Roles.Employee], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        AssertRoles(plan.ToRemove, Roles.Admin);
    }

    /// <summary>
    /// O Admin pode adicionar roles à própria conta desde que mantenha Admin.
    /// </summary>
    [TestMethod]
    public void Plan_AllowsAdminAddingRolesWhileKeepingAdmin()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Admin], [Roles.Admin, Roles.Employee], isSelf: true);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        AssertRoles(plan.ToAdd, Roles.Employee);
        Assert.IsEmpty(plan.ToRemove);
    }

    /// <summary>
    /// Só o nome exato de uma role conhecida é aceito; variações de maiúsculas ou espaços são recusadas.
    /// </summary>
    [TestMethod]
    public void Plan_RejectsRoleNameThatIsNotExact()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([], ["employee", " Employee "], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.UnknownRoles, plan.Status);
        AssertRoles(plan.UnknownRoles, "employee", " Employee ");
    }

    /// <summary>
    /// A mesma role repetida na requisição é adicionada uma única vez.
    /// </summary>
    [TestMethod]
    public void Plan_IgnoresRepeatedRoleInRequest()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([], [Roles.Employee, Roles.Employee], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        AssertRoles(plan.ToAdd, Roles.Employee);
    }

    /// <summary>
    /// Uma lista vazia remove todas as roles de outro usuário.
    /// </summary>
    [TestMethod]
    public void Plan_EmptyRequestRemovesAllRolesFromAnotherUser()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Employee, Roles.Auditor], [], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        AssertRoles(plan.ToRemove, Roles.Auditor, Roles.Employee);
        Assert.IsEmpty(plan.FinalRoles);
    }

    /// <summary>
    /// Pedir as mesmas roles que o usuário já tem não gera alteração.
    /// </summary>
    [TestMethod]
    public void Plan_SameRolesProducesNoChanges()
    {
        RoleChangePlan plan = RoleChangePlanner.Plan([Roles.Finance], [Roles.Finance], isSelf: false);

        Assert.AreEqual(RoleChangeStatus.Accepted, plan.Status);
        Assert.IsEmpty(plan.ToAdd);
        Assert.IsEmpty(plan.ToRemove);
    }

    private static void AssertRoles(IReadOnlyList<string> actual, params string[] expected)
    {
        string[] actualRoles = [.. actual];
        CollectionAssert.AreEqual(expected, actualRoles);
    }
}
