using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro auditável de uma ação executada sobre um reembolso.
/// </summary>
public class ExpenseHistory
{
    /// <summary>Tamanho máximo do nome da ação.</summary>
    public const int ActionMaxLength = 50;

    /// <summary>Tamanho máximo da justificativa de reprovação.</summary>
    public const int JustificationMaxLength = 500;

    /// <summary>Identificador do registro.</summary>
    public long Id { get; set; }

    /// <summary>Reembolso ao qual o registro pertence.</summary>
    public Guid ExpenseId { get; set; }

    /// <summary>Ação executada (criação, edição, envio, aprovação, reprovação ou pagamento).</summary>
    public required string Action { get; set; }

    /// <summary>Usuário que executou a ação, derivado da identidade autenticada.</summary>
    public required string ActorId { get; set; }

    /// <summary>Momento da ação, em UTC, definido pelo servidor.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Estado antes da ação; nulo na criação.</summary>
    public ExpenseStatus? PreviousStatus { get; set; }

    /// <summary>Estado depois da ação.</summary>
    public ExpenseStatus NewStatus { get; set; }

    /// <summary>Justificativa, usada na reprovação.</summary>
    public string? Justification { get; set; }
}
