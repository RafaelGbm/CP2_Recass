using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Reembolso devolvido pelas rotas de <c>/api/expenses</c>.
/// </summary>
public sealed class ExpenseResponse
{
    /// <summary>Identificador do reembolso.</summary>
    [Required]
    public required Guid Id { get; init; }

    /// <summary>Identificador do usuário dono.</summary>
    [Required]
    public required string OwnerId { get; init; }

    /// <summary>Descrição do gasto.</summary>
    [Required]
    public required string Description { get; init; }

    /// <summary>Valor solicitado.</summary>
    [Required]
    public required decimal Amount { get; init; }

    /// <summary>Data da despesa.</summary>
    [Required]
    public required DateOnly ExpenseDate { get; init; }

    /// <summary>Categoria, quando informada.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Nome da categoria, quando informada.</summary>
    [StringLength(100)]
    public string? CategoryName { get; init; }

    /// <summary>Estado atual: <c>Draft</c>, <c>Submitted</c>, <c>Approved</c>, <c>Rejected</c> ou <c>Paid</c>.</summary>
    [Required]
    public required string Status { get; init; }

    /// <summary>Momento de criação, em UTC.</summary>
    [Required]
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Momento da última alteração, em UTC.</summary>
    [Required]
    public required DateTime UpdatedAtUtc { get; init; }
}
