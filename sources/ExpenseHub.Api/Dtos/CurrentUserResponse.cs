using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados do usuário autenticado, conforme o token atual.
/// </summary>
public sealed class CurrentUserResponse
{
    /// <summary>Identificador do usuário.</summary>
    [Required]
    public required string Id { get; init; }

    /// <summary>E-mail do usuário.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>Roles presentes no token; mudanças de role exigem novo login.</summary>
    [Required]
    public required IReadOnlyList<string> Roles { get; init; }
}
