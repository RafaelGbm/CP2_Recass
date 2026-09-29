using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Decide quais roles adicionar e remover de um usuário, sem acessar o banco.
/// </summary>
public static class RoleChangePlanner
{
    /// <summary>
    /// Calcula a troca do conjunto atual de roles pelo conjunto solicitado.
    /// </summary>
    /// <param name="currentRoles">Roles que o usuário possui hoje.</param>
    /// <param name="requestedRoles">Conjunto completo de roles desejado.</param>
    /// <param name="isSelf">Indica se o Admin está alterando a própria conta.</param>
    /// <returns>O plano de alteração ou o motivo da recusa.</returns>
    public static RoleChangePlan Plan(
        IEnumerable<string> currentRoles,
        IEnumerable<string> requestedRoles,
        bool isSelf)
    {
        ArgumentNullException.ThrowIfNull(currentRoles);
        ArgumentNullException.ThrowIfNull(requestedRoles);

        List<string> unknown = [];
        SortedSet<string> desired = new(StringComparer.Ordinal);

        foreach (string? requested in requestedRoles)
        {
            string? known = Roles.All.FirstOrDefault(
                role => string.Equals(role, requested?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (known is null)
            {
                unknown.Add(requested ?? string.Empty);
            }
            else
            {
                desired.Add(known);
            }
        }

        if (unknown.Count > 0)
        {
            return RoleChangePlan.Rejected(RoleChangeStatus.UnknownRoles, unknown);
        }

        HashSet<string> current = new(currentRoles, StringComparer.Ordinal);

        if (isSelf && current.Contains(Roles.Admin) && !desired.Contains(Roles.Admin))
        {
            return RoleChangePlan.Rejected(RoleChangeStatus.SelfAdminRemoval, []);
        }

        return new RoleChangePlan(
            RoleChangeStatus.Accepted,
            [.. desired.Where(role => !current.Contains(role))],
            [.. current.Where(role => !desired.Contains(role)).Order(StringComparer.Ordinal)],
            [],
            [.. desired]);
    }
}

/// <summary>
/// Resultado da decisão sobre uma troca de roles.
/// </summary>
public enum RoleChangeStatus
{
    /// <summary>A troca pode ser aplicada.</summary>
    Accepted = 0,

    /// <summary>Alguma role solicitada não existe no ExpenseHub.</summary>
    UnknownRoles = 1,

    /// <summary>O Admin tentou remover a própria role Admin.</summary>
    SelfAdminRemoval = 2,
}

/// <summary>
/// Plano de alteração de roles calculado por <see cref="RoleChangePlanner"/>.
/// </summary>
/// <param name="Status">Se a troca foi aceita ou o motivo da recusa.</param>
/// <param name="ToAdd">Roles que precisam ser adicionadas.</param>
/// <param name="ToRemove">Roles que precisam ser removidas.</param>
/// <param name="UnknownRoles">Roles solicitadas que não existem.</param>
/// <param name="FinalRoles">Roles que o usuário terá depois da troca.</param>
public sealed record RoleChangePlan(
    RoleChangeStatus Status,
    IReadOnlyList<string> ToAdd,
    IReadOnlyList<string> ToRemove,
    IReadOnlyList<string> UnknownRoles,
    IReadOnlyList<string> FinalRoles)
{
    /// <summary>
    /// Cria um plano recusado, sem alterações.
    /// </summary>
    /// <param name="status">Motivo da recusa.</param>
    /// <param name="unknownRoles">Roles desconhecidas, quando houver.</param>
    /// <returns>O plano recusado.</returns>
    public static RoleChangePlan Rejected(RoleChangeStatus status, IReadOnlyList<string> unknownRoles) =>
        new(status, [], [], unknownRoles, []);
}
