using System;
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
/// Rotas de reembolso. As regras de dono e estado ficam em <see cref="ExpenseService"/>.
/// </summary>
/// <param name="service">Serviço de reembolsos.</param>
[ApiController]
[Route("api/expenses")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class ExpensesController(ExpenseService service) : ControllerBase
{
    /// <summary>
    /// Cria um rascunho. O dono é o usuário autenticado e o estado inicial é Draft.
    /// </summary>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho criado.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> Create(
        [FromBody] ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.CreateAsync(CurrentUserId(), request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status201Created);
    }

    /// <summary>
    /// Edita um rascunho do próprio usuário. Só é permitido em Draft.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho atualizado.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id,
        [FromBody] ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.UpdateAsync(id, CurrentUserId(), request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private ActionResult<ExpenseResponse> ToActionResult(ExpenseOperationResult result, int successStatusCode)
    {
        switch (result.Status)
        {
            case ExpenseOperationStatus.Success:
                return StatusCode(successStatusCode, result.Expense);

            case ExpenseOperationStatus.NotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Reembolso não encontrado.");

            case ExpenseOperationStatus.Conflict:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "O estado atual do reembolso não permite esta operação.");

            case ExpenseOperationStatus.InvalidCategory:
                ModelState.AddModelError(nameof(ExpenseDraftRequest.CategoryId), "Categoria inexistente.");
                return ValidationProblem(ModelState);

            default:
                throw new InvalidOperationException($"Resultado não tratado: {result.Status}.");
        }
    }
}
