using System.Linq;
using System.Security.Claims;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Informações do usuário autenticado.
/// </summary>
[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    /// <summary>
    /// Retorna identificador, e-mail e roles do token atual.
    /// </summary>
    /// <returns>Os dados do usuário autenticado.</returns>
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Get()
    {
        return new CurrentUserResponse
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty,
            Roles = [.. User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Order()],
        };
    }
}
