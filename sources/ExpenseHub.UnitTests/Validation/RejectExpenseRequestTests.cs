using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Validation;

/// <summary>
/// Validação declarativa da justificativa de reprovação.
/// </summary>
[TestClass]
public sealed class RejectExpenseRequestTests
{
    /// <summary>
    /// A justificativa precisa ter entre 10 e 500 caracteres.
    /// </summary>
    /// <param name="length">Tamanho da justificativa.</param>
    /// <param name="expectedValid">Se o tamanho é aceito.</param>
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void Validate_JustificationLength_RespectsLimits(int length, bool expectedValid)
    {
        RejectExpenseRequest request = new() { Justification = new string('x', length) };

        Assert.AreEqual(expectedValid, !HasJustificationError(request));
    }

    /// <summary>
    /// Justificativa só com espaços é tratada como ausente.
    /// </summary>
    [TestMethod]
    public void Validate_WhitespaceJustification_IsRejected()
    {
        Assert.IsTrue(HasJustificationError(new RejectExpenseRequest { Justification = new string(' ', 20) }));
    }

    /// <summary>
    /// Sem o campo no JSON, a justificativa fica vazia e é recusada.
    /// </summary>
    [TestMethod]
    public void Deserialize_MissingJustification_IsRejected()
    {
        RejectExpenseRequest request = JsonSerializer.Deserialize<RejectExpenseRequest>("{}", JsonSerializerOptions.Web)!;

        Assert.IsTrue(HasJustificationError(request));
    }

    /// <summary>
    /// O cliente não informa ator, horário nem estado: campos extras no JSON são recusados.
    /// </summary>
    /// <param name="extraField">Campo que o cliente tenta enviar.</param>
    [TestMethod]
    [DataRow("\"actorId\":\"outro-usuario\"")]
    [DataRow("\"occurredAtUtc\":\"2026-01-01T00:00:00Z\"")]
    [DataRow("\"status\":\"Approved\"")]
    public void Deserialize_ServerControlledField_IsRejected(string extraField)
    {
        string json = "{\"justification\":\"Nota fiscal ilegível\"," + extraField + "}";

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<RejectExpenseRequest>(json, JsonSerializerOptions.Web));
    }

    private static bool HasJustificationError(RejectExpenseRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.Any(result => result.MemberNames.Contains(nameof(RejectExpenseRequest.Justification)));
    }
}
