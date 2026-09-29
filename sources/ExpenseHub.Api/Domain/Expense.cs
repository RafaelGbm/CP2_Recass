using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Reembolso corporativo com um único valor.
/// </summary>
public class Expense
{
    /// <summary>Identificador do reembolso.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário dono, sempre derivado da identidade autenticada.</summary>
    public required string OwnerId { get; set; }

    /// <summary>Descrição do gasto.</summary>
    public required string Description { get; set; }

    /// <summary>Valor solicitado, em reais.</summary>
    public decimal Amount { get; set; }

    /// <summary>Data em que a despesa ocorreu.</summary>
    public DateOnly ExpenseDate { get; set; }

    /// <summary>Identificador da categoria, quando informada.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Categoria do reembolso.</summary>
    public ExpenseCategory? Category { get; set; }

    /// <summary>Estado atual, controlado pelo servidor.</summary>
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    /// <summary>Momento de criação, em UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Momento da última alteração, em UTC.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}
