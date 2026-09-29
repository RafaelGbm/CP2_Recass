using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro do pagamento simulado de um reembolso aprovado.
/// </summary>
public class PaymentRecord
{
    /// <summary>Identificador do registro.</summary>
    public long Id { get; set; }

    /// <summary>Reembolso pago; cada reembolso possui no máximo um pagamento.</summary>
    public Guid ExpenseId { get; set; }

    /// <summary>Usuário do Finance que registrou o pagamento.</summary>
    public required string PaidById { get; set; }

    /// <summary>Momento do pagamento, em UTC, definido pelo servidor.</summary>
    public DateTime PaidAtUtc { get; set; }
}
