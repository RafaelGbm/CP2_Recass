using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de negócio dos reembolsos. Dono, estado, ator e horários são sempre definidos aqui, nunca pelo cliente.
/// </summary>
/// <param name="db">Contexto do banco.</param>
public sealed class ExpenseService(ExpenseHubDbContext db)
{
    /// <summary>
    /// Cria um rascunho do usuário autenticado e grava o histórico de criação na mesma operação.
    /// </summary>
    /// <param name="userId">Usuário autenticado, que será o dono.</param>
    /// <param name="request">Dados do rascunho, já validados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho criado, ou <see cref="ExpenseOperationStatus.InvalidCategory"/>.</returns>
    public async Task<ExpenseOperationResult> CreateAsync(
        string userId,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        (bool Found, string? Name) category = await FindCategoryAsync(request.CategoryId, cancellationToken);
        if (!category.Found)
        {
            return ExpenseOperationResult.Failed(ExpenseOperationStatus.InvalidCategory);
        }

        DateTime now = DateTime.UtcNow;
        Expense expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Description = request.Description,
            Amount = request.Amount.GetValueOrDefault(),
            ExpenseDate = request.ExpenseDate.GetValueOrDefault(),
            CategoryId = request.CategoryId,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.Expenses.Add(expense);
        db.ExpenseHistories.Add(new ExpenseHistory
        {
            ExpenseId = expense.Id,
            Action = ExpenseActions.Created,
            ActorId = userId,
            OccurredAtUtc = now,
            PreviousStatus = null,
            NewStatus = ExpenseStatus.Draft,
        });

        await db.SaveChangesAsync(cancellationToken);

        return ExpenseOperationResult.Succeeded(ToResponse(expense, category.Name));
    }

    /// <summary>
    /// Edita um rascunho do próprio usuário. Sem alterações efetivas, nada é gravado.
    /// </summary>
    /// <param name="expenseId">Reembolso editado.</param>
    /// <param name="userId">Usuário autenticado.</param>
    /// <param name="request">Novos dados do rascunho, já validados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>
    /// O rascunho atualizado; <see cref="ExpenseOperationStatus.NotFound"/> se não existir ou for de outro usuário;
    /// <see cref="ExpenseOperationStatus.Conflict"/> se não estiver em Draft;
    /// <see cref="ExpenseOperationStatus.InvalidCategory"/> se a categoria não existir.
    /// </returns>
    public async Task<ExpenseOperationResult> UpdateAsync(
        Guid expenseId,
        string userId,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // O filtro por dono fica na consulta: reembolso de outro usuário é tratado como inexistente.
        Expense? expense = await db.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.OwnerId == userId, cancellationToken);
        if (expense is null)
        {
            return ExpenseOperationResult.Failed(ExpenseOperationStatus.NotFound);
        }

        if (!ExpenseWorkflow.CanEdit(expense.Status))
        {
            return ExpenseOperationResult.Failed(ExpenseOperationStatus.Conflict);
        }

        (bool Found, string? Name) category = await FindCategoryAsync(request.CategoryId, cancellationToken);
        if (!category.Found)
        {
            return ExpenseOperationResult.Failed(ExpenseOperationStatus.InvalidCategory);
        }

        ExpenseDraftValues before = new(expense.Description, expense.Amount, expense.ExpenseDate, expense.CategoryId);
        ExpenseDraftValues after = new(
            request.Description,
            request.Amount.GetValueOrDefault(),
            request.ExpenseDate.GetValueOrDefault(),
            request.CategoryId);

        string? changes = ExpenseChangeDescriber.Describe(before, after);
        if (changes is null)
        {
            return ExpenseOperationResult.Succeeded(ToResponse(expense, category.Name));
        }

        DateTime now = DateTime.UtcNow;
        expense.Description = after.Description;
        expense.Amount = after.Amount;
        expense.ExpenseDate = after.ExpenseDate;
        expense.CategoryId = after.CategoryId;
        expense.UpdatedAtUtc = now;

        db.ExpenseHistories.Add(new ExpenseHistory
        {
            ExpenseId = expense.Id,
            Action = ExpenseActions.Updated,
            ActorId = userId,
            OccurredAtUtc = now,
            PreviousStatus = ExpenseStatus.Draft,
            NewStatus = ExpenseStatus.Draft,
            Changes = changes,
        });

        await db.SaveChangesAsync(cancellationToken);

        return ExpenseOperationResult.Succeeded(ToResponse(expense, category.Name));
    }

    private static ExpenseResponse ToResponse(Expense expense, string? categoryName) => new()
    {
        Id = expense.Id,
        OwnerId = expense.OwnerId,
        Description = expense.Description,
        Amount = expense.Amount,
        ExpenseDate = expense.ExpenseDate,
        CategoryId = expense.CategoryId,
        CategoryName = categoryName,
        Status = expense.Status.ToString(),
        CreatedAtUtc = expense.CreatedAtUtc,
        UpdatedAtUtc = expense.UpdatedAtUtc,
    };

    private async Task<(bool Found, string? Name)> FindCategoryAsync(int? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            return (true, null);
        }

        string? name = await db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.Id == categoryId.Value)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return (name is not null, name);
    }
}

/// <summary>
/// Resultado de uma operação de <see cref="ExpenseService"/>.
/// </summary>
/// <param name="Status">Sucesso ou motivo da recusa.</param>
/// <param name="Expense">Reembolso resultante, quando a operação foi aplicada.</param>
public sealed record ExpenseOperationResult(ExpenseOperationStatus Status, ExpenseResponse? Expense)
{
    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    /// <param name="expense">Reembolso resultante.</param>
    /// <returns>O resultado.</returns>
    public static ExpenseOperationResult Succeeded(ExpenseResponse expense) => new(ExpenseOperationStatus.Success, expense);

    /// <summary>
    /// Cria um resultado de recusa, sem alterações.
    /// </summary>
    /// <param name="status">Motivo da recusa.</param>
    /// <returns>O resultado.</returns>
    public static ExpenseOperationResult Failed(ExpenseOperationStatus status) => new(status, null);
}

/// <summary>
/// Desfecho de uma operação sobre reembolso, traduzido em status HTTP pelo controller.
/// </summary>
public enum ExpenseOperationStatus
{
    /// <summary>A operação foi aplicada.</summary>
    Success = 0,

    /// <summary>Reembolso inexistente ou fora do escopo do usuário (404).</summary>
    NotFound = 1,

    /// <summary>O estado atual não permite a operação (409).</summary>
    Conflict = 2,

    /// <summary>A categoria informada não existe (400).</summary>
    InvalidCategory = 3,
}
