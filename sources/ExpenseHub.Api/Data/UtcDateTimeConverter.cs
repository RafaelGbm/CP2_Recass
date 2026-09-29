using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Grava instantes em UTC e os devolve marcados como UTC. O SQLite guarda datas como texto
/// e, sem esta conversão, o valor lido perderia a indicação de fuso.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Cria o conversor.
    /// </summary>
    public UtcDateTimeConverter()
        : base(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    /// <summary>
    /// Converte para UTC; um valor sem fuso definido é tratado como UTC.
    /// </summary>
    /// <param name="value">Instante a gravar.</param>
    /// <returns>O instante em UTC.</returns>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
