using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Administração de usuários e roles, restrita ao Admin.
/// </summary>
/// <param name="service">Serviço de administração de usuários.</param>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AdminUsersController(UserAdministrationService service) : ControllerBase
{
    /// <summary>
    /// Lista os usuários e suas roles.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários cadastrados.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListUsersAsync(cancellationToken));
    }

    /// <summary>
    /// Substitui as roles de um usuário. O usuário precisa fazer login de novo para o token refletir a mudança.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="request">Conjunto completo de roles desejado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuário com as roles atualizadas.</returns>
    [HttpPut("{id}/roles")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateRoles(
        string id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        string actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        RoleUpdateResult result = await service.UpdateRolesAsync(id, request.Roles, actingUserId, cancellationToken);

        if (!result.UserFound)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado.");
        }

        switch (result.Plan?.Status)
        {
            case RoleChangeStatus.UnknownRoles:
                ModelState.AddModelError(
                    nameof(request.Roles),
                    $"Roles desconhecidas: {string.Join(", ", result.Plan.UnknownRoles)}. Válidas: {string.Join(", ", Roles.All)}.");
                return ValidationProblem(ModelState);

            case RoleChangeStatus.SelfAdminRemoval:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "O Admin não pode remover a própria role Admin.");

            default:
                return Ok(result.User);
        }
    }
}
