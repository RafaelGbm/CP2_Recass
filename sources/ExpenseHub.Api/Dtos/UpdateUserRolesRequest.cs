using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto completo de roles que o usuário deve ter depois da alteração.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>Roles desejadas; uma lista vazia remove todas as roles.</summary>
    [Required]
    [MaxLength(10)]
    public IReadOnlyList<string> Roles { get; init; } = [];
}
