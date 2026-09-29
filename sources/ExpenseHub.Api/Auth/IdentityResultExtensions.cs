using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Utilitários para resultados do Identity.
/// </summary>
internal static class IdentityResultExtensions
{
    /// <summary>
    /// Lança exceção quando uma operação interna do Identity falha.
    /// </summary>
    /// <param name="result">Resultado da operação.</param>
    /// <param name="operation">Descrição da operação, usada na mensagem.</param>
    public static void ThrowIfFailed(this IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao {operation}: {errors}");
        }
    }
}
