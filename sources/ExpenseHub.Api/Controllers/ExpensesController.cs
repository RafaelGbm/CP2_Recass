using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;
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
    private const string ReadRoles = Roles.Employee + "," + Roles.Approver + "," + Roles.Finance + "," + Roles.Auditor;

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
        ExpenseOperationResult result = await service.UpdateAsync(id, CurrentViewer(), request, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Lista os reembolsos visíveis: Employee vê os próprios, Approver os enviados, Finance os aprovados e pagos, Auditor todos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos dentro do escopo de leitura.</returns>
    [HttpGet]
    [Authorize(Roles = ReadRoles)]
    [ProducesResponseType<IReadOnlyList<ExpenseResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(CurrentViewer(), cancellationToken));
    }

    /// <summary>
    /// Consulta um reembolso dentro do escopo de leitura.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = ReadRoles)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.GetAsync(id, CurrentViewer(), cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Envia o próprio rascunho para aprovação (Draft para Submitted).
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado.</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.SubmitAsync(id, CurrentViewer(), cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Aprova um reembolso enviado por outra pessoa (Submitted para Approved).
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado.</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Approver)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.ApproveAsync(id, CurrentViewer(), cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Reprova um reembolso enviado por outra pessoa (Submitted para Rejected), com justificativa obrigatória.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa da reprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado.</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Approver)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseResponse>> Reject(
        Guid id,
        [FromBody] RejectExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ExpenseOperationResult result = await service.RejectAsync(
            id,
            CurrentViewer(),
            request.Justification,
            cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private ExpenseViewer CurrentViewer() => new(
        CurrentUserId(),
        User.IsInRole(Roles.Employee),
        User.IsInRole(Roles.Approver),
        User.IsInRole(Roles.Finance),
        User.IsInRole(Roles.Auditor));

    private ActionResult<ExpenseResponse> ToActionResult(ExpenseOperationResult result, int successStatusCode)
    {
        switch (result.Status)
        {
            case ExpenseOperationStatus.Success:
                return StatusCode(successStatusCode, result.Expense);

            case ExpenseOperationStatus.NotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Reembolso não encontrado.");

            case ExpenseOperationStatus.Forbidden:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "O usuário não pode executar esta operação sobre o reembolso.");

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
