namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria à qual um reembolso pertence.
/// </summary>
public class ExpenseCategory
{
    /// <summary>Tamanho máximo do nome da categoria.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Identificador da categoria.</summary>
    public int Id { get; set; }

    /// <summary>Nome único da categoria.</summary>
    public required string Name { get; set; }
}
