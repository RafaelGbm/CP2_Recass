using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Autenticação por token bearer.
/// </summary>
/// <param name="signInManager">Gerenciador de login do Identity.</param>
[ApiController]
[AllowAnonymous]
public sealed class AuthController(SignInManager<IdentityUser> signInManager) : ControllerBase
{
    /// <summary>
    /// Valida as credenciais e emite um token bearer.
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>O token de acesso, ou 401 para credenciais inválidas.</returns>
    [HttpPost("/login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Credenciais inválidas.");
        }

        // O handler bearer já escreveu o token na resposta durante o SignIn.
        return new EmptyResult();
    }
}
