using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Descreve as alterações de um rascunho para o campo <see cref="ExpenseHistory.Changes"/>, sem acessar o banco.
/// </summary>
public static class ExpenseChangeDescriber
{
    /// <summary>
    /// Compara os valores atuais com os novos.
    /// </summary>
    /// <param name="before">Valores antes da edição.</param>
    /// <param name="after">Valores enviados na edição.</param>
    /// <returns>A descrição das alterações, ou <see langword="null"/> quando nada mudou.</returns>
    public static string? Describe(ExpenseDraftValues before, ExpenseDraftValues after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        List<string> changes = [];

        if (!string.Equals(before.Description, after.Description, StringComparison.Ordinal))
        {
            changes.Add($"description: \"{before.Description}\" -> \"{after.Description}\"");
        }

        if (before.Amount != after.Amount)
        {
            changes.Add($"amount: {FormatAmount(before.Amount)} -> {FormatAmount(after.Amount)}");
        }

        if (before.ExpenseDate != after.ExpenseDate)
        {
            changes.Add($"expenseDate: {FormatDate(before.ExpenseDate)} -> {FormatDate(after.ExpenseDate)}");
        }

        if (before.CategoryId != after.CategoryId)
        {
            changes.Add($"categoryId: {FormatCategory(before.CategoryId)} -> {FormatCategory(after.CategoryId)}");
        }

        return changes.Count == 0 ? null : string.Join("; ", changes);
    }

    private static string FormatAmount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatCategory(int? categoryId) =>
        categoryId?.ToString(CultureInfo.InvariantCulture) ?? "null";
}

/// <summary>
/// Campos editáveis de um rascunho.
/// </summary>
/// <param name="Description">Descrição do gasto.</param>
/// <param name="Amount">Valor solicitado.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="CategoryId">Categoria, quando informada.</param>
public sealed record ExpenseDraftValues(string Description, decimal Amount, DateOnly ExpenseDate, int? CategoryId);
