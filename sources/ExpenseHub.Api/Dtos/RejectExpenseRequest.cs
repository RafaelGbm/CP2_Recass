using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos na reprovação (<c>POST /api/expenses/{id}/reject</c>). Ator e horário vêm do servidor:
/// campos extras são recusados com 400.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RejectExpenseRequest
{
    /// <summary>Justificativa da reprovação, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; init; } = string.Empty;
}
