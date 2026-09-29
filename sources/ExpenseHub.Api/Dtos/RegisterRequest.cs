using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos por <c>POST /register</c>. Campos extras, como roles, são recusados com 400.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterRequest
{
    /// <summary>E-mail do novo usuário, usado também como nome de usuário.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Senha, validada também pela política do Identity.</summary>
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; init; } = string.Empty;
}
