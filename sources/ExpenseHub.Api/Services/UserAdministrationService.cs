using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Operações administrativas sobre usuários e roles.
/// </summary>
/// <param name="db">Contexto do banco.</param>
/// <param name="userManager">Gerenciador de usuários do Identity.</param>
public sealed class UserAdministrationService(ExpenseHubDbContext db, UserManager<IdentityUser> userManager)
{
    /// <summary>
    /// Lista todos os usuários com suas roles, em duas consultas.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários ordenados por e-mail.</returns>
    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var userRoles = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ILookup<string, string> rolesByUser = userRoles.ToLookup(item => item.UserId, item => item.Name ?? string.Empty);

        return [.. users.Select(user => new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = [.. rolesByUser[user.Id].Order()],
        })];
    }

    /// <summary>
    /// Substitui as roles de um usuário pelo conjunto solicitado.
    /// </summary>
    /// <param name="userId">Usuário alterado.</param>
    /// <param name="requestedRoles">Conjunto completo de roles desejado.</param>
    /// <param name="actingUserId">Admin que executa a alteração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O resultado da alteração.</returns>
    public async Task<RoleUpdateResult> UpdateRolesAsync(
        string userId,
        IReadOnlyCollection<string> requestedRoles,
        string actingUserId,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new RoleUpdateResult(false, null, null);
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        RoleChangePlan plan = RoleChangePlanner.Plan(currentRoles, requestedRoles, isSelf: user.Id == actingUserId);
        if (plan.Status != RoleChangeStatus.Accepted)
        {
            return new RoleUpdateResult(true, plan, null);
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (plan.ToRemove.Count > 0)
        {
            (await userManager.RemoveFromRolesAsync(user, plan.ToRemove)).ThrowIfFailed("remover roles");
        }

        if (plan.ToAdd.Count > 0)
        {
            (await userManager.AddToRolesAsync(user, plan.ToAdd)).ThrowIfFailed("adicionar roles");
        }

        if (plan.ToAdd.Count > 0 || plan.ToRemove.Count > 0)
        {
            // Invalida os tokens emitidos com as roles antigas: o usuário precisa autenticar novamente.
            (await userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed("renovar o carimbo de segurança");
        }

        await transaction.CommitAsync(cancellationToken);

        UserResponse response = new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = plan.FinalRoles,
        };

        return new RoleUpdateResult(true, plan, response);
    }
}

/// <summary>
/// Resultado de <see cref="UserAdministrationService.UpdateRolesAsync"/>.
/// </summary>
/// <param name="UserFound">Indica se o usuário existe.</param>
/// <param name="Plan">Plano calculado; nulo quando o usuário não existe.</param>
/// <param name="User">Usuário atualizado, quando a alteração foi aplicada.</param>
public sealed record RoleUpdateResult(bool UserFound, RoleChangePlan? Plan, UserResponse? User);
