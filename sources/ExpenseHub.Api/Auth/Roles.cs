using System.Collections.Generic;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Roles conhecidas pelo ExpenseHub. Nenhuma outra role é criada pela aplicação.
/// </summary>
public static class Roles
{
    /// <summary>Administra usuários e roles; não possui acesso funcional aos reembolsos.</summary>
    public const string Admin = "Admin";

    /// <summary>Cria, edita, envia e consulta os próprios reembolsos.</summary>
    public const string Employee = "Employee";

    /// <summary>Aprova ou reprova reembolsos enviados por outras pessoas.</summary>
    public const string Approver = "Approver";

    /// <summary>Registra o pagamento de reembolsos aprovados de outras pessoas.</summary>
    public const string Finance = "Finance";

    /// <summary>Consulta todos os reembolsos e históricos, sem alterar dados.</summary>
    public const string Auditor = "Auditor";

    /// <summary>Todas as roles conhecidas.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Employee, Approver, Finance, Auditor];
}
