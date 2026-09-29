namespace ExpenseHub.Api.Auth;

/// <summary>
/// Configuração da conta Admin inicial, lida da seção <c>Seed</c>.
/// </summary>
/// <remarks>
/// A senha nunca fica no código ou em arquivos versionados: ela vem de User Secrets
/// (<c>Seed:AdminPassword</c>) ou da variável de ambiente <c>Seed__AdminPassword</c>.
/// </remarks>
public sealed class AdminSeedOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Seed";

    /// <summary>E-mail da conta Admin, usado também como nome de usuário.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>Senha inicial da conta Admin.</summary>
    public string? AdminPassword { get; set; }
}
