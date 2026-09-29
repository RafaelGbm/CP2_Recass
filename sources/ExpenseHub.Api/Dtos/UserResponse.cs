using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Usuário e suas roles atuais.
/// </summary>
public sealed class UserResponse
{
    /// <summary>Identificador do usuário.</summary>
    [Required]
    public required string Id { get; init; }

    /// <summary>E-mail do usuário.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>Roles atribuídas ao usuário.</summary>
    [Required]
    public required IReadOnlyList<string> Roles { get; init; }
}
