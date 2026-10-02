using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Regra de "data não futura" da despesa. O dia de referência é o de Brasília, o fuso dos usuários.
/// </summary>
public static class ExpenseDateRules
{
    /// <summary>Identificador IANA do fuso de Brasília.</summary>
    public const string BrazilTimeZoneId = "America/Sao_Paulo";

    private static readonly TimeZoneInfo _brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById(BrazilTimeZoneId);

    /// <summary>
    /// Converte um instante para a data corrente em Brasília.
    /// </summary>
    /// <param name="now">Instante de referência.</param>
    /// <returns>A data em Brasília nesse instante.</returns>
    public static DateOnly TodayInBrazil(DateTimeOffset now) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, _brazilTimeZone).DateTime);

    /// <summary>
    /// Indica se a data da despesa não é posterior ao dia de referência.
    /// </summary>
    /// <param name="expenseDate">Data informada.</param>
    /// <param name="today">Dia de referência.</param>
    /// <returns><see langword="true"/> para hoje ou datas passadas.</returns>
    public static bool IsNotFuture(DateOnly expenseDate, DateOnly today) => expenseDate <= today;
}
