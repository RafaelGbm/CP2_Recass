using System;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Validation;

/// <summary>
/// Recusa datas posteriores ao dia corrente em Brasília (veja <see cref="ExpenseDateRules"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotFutureDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Cria o atributo com a mensagem padrão.
    /// </summary>
    public NotFutureDateAttribute()
        : base("O campo {0} não pode ser uma data futura.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is not DateOnly date || ExpenseDateRules.IsNotFuture(date, ExpenseDateRules.TodayInBrazil(DateTimeOffset.UtcNow));
}
