using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Cria as roles conhecidas e a única conta Admin inicial. Pode ser executado várias vezes.
/// </summary>
/// <param name="roleManager">Gerenciador de roles do Identity.</param>
/// <param name="userManager">Gerenciador de usuários do Identity.</param>
/// <param name="options">Configuração da conta Admin inicial.</param>
/// <param name="logger">Logger do seed.</param>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    IOptions<AdminSeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    /// <summary>
    /// Garante as roles e a conta Admin sem duplicar registros já existentes.
    /// </summary>
    /// <returns>Tarefa assíncrona do seed.</returns>
    public async Task SeedAsync()
    {
        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"criar a role {role}");
            }
        }

        IList<IdentityUser> admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        if (admins.Count > 0)
        {
            return;
        }

        AdminSeedOptions seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogWarning(
                "Conta Admin não criada: configure {EmailKey} e {PasswordKey} (User Secrets ou variável de ambiente).",
                "Seed:AdminEmail",
                "Seed:AdminPassword");
            return;
        }

        if (await userManager.FindByEmailAsync(seed.AdminEmail) is not null)
        {
            logger.LogWarning("Conta Admin não criada: já existe um usuário sem role Admin com o e-mail configurado.");
            return;
        }

        IdentityUser admin = new()
        {
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            EmailConfirmed = true,
        };

        EnsureSucceeded(await userManager.CreateAsync(admin, seed.AdminPassword), "criar a conta Admin");
        EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin), "atribuir a role Admin");
        logger.LogInformation("Conta Admin inicial criada.");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao {operation}: {errors}");
        }
    }
}
