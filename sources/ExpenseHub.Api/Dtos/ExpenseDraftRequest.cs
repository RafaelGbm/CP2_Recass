using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ExpenseHub.Api.Validation;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos na criação (<c>POST /api/expenses</c>) e na edição (<c>PUT /api/expenses/{id}</c>) de um rascunho.
/// Dono, estado, ator e horários vêm do servidor: campos extras, como <c>ownerId</c> ou <c>status</c>, são recusados com 400.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ExpenseDraftRequest
{
    /// <summary>Descrição do gasto, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Valor solicitado, entre R$ 0,01 e <see cref="int.MaxValue"/>.</summary>
    [Required]
    [Range(
        typeof(decimal),
        "0.01",
        "2147483647",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; init; }

    /// <summary>Data da despesa (<c>yyyy-MM-dd</c>), que não pode ser futura.</summary>
    [Required]
    [NotFutureDate]
    public DateOnly? ExpenseDate { get; init; }

    /// <summary>Categoria opcional; precisa existir quando informada.</summary>
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }
}
