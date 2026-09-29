using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Recusa tokens emitidos antes de uma alteração de roles do usuário.
/// </summary>
internal static class SecurityStampValidationExtensions
{
    /// <summary>
    /// Compara o carimbo de segurança do token com o do banco. Quando as roles mudam, o carimbo é renovado
    /// e o token antigo passa a receber 401, obrigando o usuário a autenticar novamente.
    /// </summary>
    /// <param name="app">Pipeline da aplicação.</param>
    /// <returns>O mesmo pipeline, para encadeamento.</returns>
    public static IApplicationBuilder UseSecurityStampValidation(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                SignInManager<IdentityUser> signInManager =
                    context.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();

                if (await signInManager.ValidateSecurityStampAsync(context.User) is null)
                {
                    await Results.Problem(
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Credencial desatualizada. Faça login novamente.")
                        .ExecuteAsync(context);
                    return;
                }
            }

            await next(context);
        });
    }
}
