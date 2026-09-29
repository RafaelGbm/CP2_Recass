namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados possíveis de um reembolso. O estado é sempre controlado pelo servidor.
/// </summary>
public enum ExpenseStatus
{
    /// <summary>Rascunho, editável apenas pelo dono.</summary>
    Draft = 0,

    /// <summary>Enviado e aguardando decisão de um Approver.</summary>
    Submitted = 1,

    /// <summary>Aprovado e aguardando pagamento do Finance.</summary>
    Approved = 2,

    /// <summary>Reprovado (estado final).</summary>
    Rejected = 3,

    /// <summary>Pago (estado final).</summary>
    Paid = 4,
}
