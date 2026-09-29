using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Cadastro de usuários e autenticação por token bearer.
/// </summary>
/// <param name="signInManager">Gerenciador de login do Identity.</param>
/// <param name="userManager">Gerenciador de usuários do Identity.</param>
[ApiController]
[AllowAnonymous]
public sealed class AuthController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) : ControllerBase
{
    /// <summary>
    /// Cadastra um usuário sem nenhuma role. Roles são atribuídas apenas pelo Admin.
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>O usuário criado, ou 400 com os erros de validação.</returns>
    [HttpPost("/register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
    {
        IdentityUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        UserResponse response = new()
        {
            Id = user.Id,
            Email = request.Email,
            Roles = [],
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

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
