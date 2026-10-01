using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Registro do histórico devolvido por <c>GET /api/expenses/{id}/history</c>.
/// </summary>
public sealed class ExpenseHistoryResponse
{
    /// <summary>Ação: <c>Created</c>, <c>Updated</c>, <c>Submitted</c>, <c>Approved</c>, <c>Rejected</c> ou <c>Paid</c>.</summary>
    [Required]
    [StringLength(50)]
    public required string Action { get; init; }

    /// <summary>Usuário que executou a ação.</summary>
    [Required]
    public required string ActorId { get; init; }

    /// <summary>Momento da ação, em UTC.</summary>
    [Required]
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>Estado antes da ação; nulo na criação.</summary>
    [StringLength(20)]
    public string? PreviousStatus { get; init; }

    /// <summary>Estado depois da ação.</summary>
    [Required]
    public required string NewStatus { get; init; }

    /// <summary>Justificativa, na reprovação.</summary>
    [StringLength(500)]
    public string? Justification { get; init; }

    /// <summary>Alterações feitas em Draft, na edição.</summary>
    [StringLength(2000)]
    public string? Changes { get; init; }
}
