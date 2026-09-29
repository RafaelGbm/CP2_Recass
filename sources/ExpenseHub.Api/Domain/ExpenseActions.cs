namespace ExpenseHub.Api.Domain;

/// <summary>
/// Nomes das ações gravados em <see cref="ExpenseHistory.Action"/>.
/// </summary>
public static class ExpenseActions
{
    /// <summary>Criação do rascunho.</summary>
    public const string Created = "Created";

    /// <summary>Edição do rascunho.</summary>
    public const string Updated = "Updated";

    /// <summary>Envio do rascunho para aprovação.</summary>
    public const string Submitted = "Submitted";

    /// <summary>Aprovação por um Approver.</summary>
    public const string Approved = "Approved";

    /// <summary>Reprovação por um Approver.</summary>
    public const string Rejected = "Rejected";

    /// <summary>Registro do pagamento pelo Finance.</summary>
    public const string Paid = "Paid";
}
