using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Validation;

/// <summary>
/// Validação declarativa dos dados de criação e edição do rascunho.
/// </summary>
[TestClass]
public sealed class ExpenseDraftRequestTests
{
    /// <summary>
    /// Um rascunho com todos os campos dentro das regras é válido.
    /// </summary>
    [TestMethod]
    public void Validate_ValidRequest_HasNoErrors()
    {
        Assert.IsEmpty(Validate(ValidRequest()));
    }

    /// <summary>
    /// A descrição precisa ter entre 10 e 500 caracteres.
    /// </summary>
    /// <param name="length">Tamanho da descrição.</param>
    /// <param name="expectedValid">Se o tamanho é aceito.</param>
    [TestMethod]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void Validate_DescriptionLength_RespectsLimits(int length, bool expectedValid)
    {
        ExpenseDraftRequest request = ValidRequest(description: new string('x', length));

        Assert.AreEqual(expectedValid, !HasErrorFor(request, nameof(ExpenseDraftRequest.Description)));
    }

    /// <summary>
    /// Descrição só com espaços é tratada como ausente.
    /// </summary>
    [TestMethod]
    public void Validate_WhitespaceDescription_IsRejected()
    {
        Assert.IsTrue(HasErrorFor(ValidRequest(description: new string(' ', 20)), nameof(ExpenseDraftRequest.Description)));
    }

    /// <summary>
    /// O valor vai de R$ 0,01 a Int32.MaxValue, inclusive em um sistema configurado em português.
    /// </summary>
    /// <param name="amount">Valor informado, em texto invariante.</param>
    /// <param name="expectedValid">Se o valor é aceito.</param>
    [TestMethod]
    [DataRow("0", false)]
    [DataRow("-5", false)]
    [DataRow("0.01", true)]
    [DataRow("2147483647", true)]
    [DataRow("2147483647.01", false)]
    public void Validate_AmountRange_IndependsOfCulture(string amount, bool expectedValid)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            ExpenseDraftRequest request = ValidRequest(amount: decimal.Parse(amount, CultureInfo.InvariantCulture));

            Assert.AreEqual(expectedValid, !HasErrorFor(request, nameof(ExpenseDraftRequest.Amount)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    /// <summary>
    /// Valor e data são obrigatórios.
    /// </summary>
    [TestMethod]
    public void Validate_MissingAmountAndDate_AreRejected()
    {
        ExpenseDraftRequest request = new() { Description = "Almoço com cliente" };

        Assert.IsTrue(HasErrorFor(request, nameof(ExpenseDraftRequest.Amount)));
        Assert.IsTrue(HasErrorFor(request, nameof(ExpenseDraftRequest.ExpenseDate)));
    }

    /// <summary>
    /// Data futura é recusada.
    /// </summary>
    [TestMethod]
    public void Validate_FutureDate_IsRejected()
    {
        ExpenseDraftRequest request = ValidRequest(expenseDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2));

        Assert.IsTrue(HasErrorFor(request, nameof(ExpenseDraftRequest.ExpenseDate)));
    }

    /// <summary>
    /// Categoria é opcional, mas precisa ser um identificador positivo quando informada.
    /// </summary>
    [TestMethod]
    public void Validate_CategoryId_IsOptionalButPositive()
    {
        Assert.IsFalse(HasErrorFor(ValidRequest(categoryId: null), nameof(ExpenseDraftRequest.CategoryId)));
        Assert.IsTrue(HasErrorFor(ValidRequest(categoryId: 0), nameof(ExpenseDraftRequest.CategoryId)));
    }

    /// <summary>
    /// O cliente não informa dono nem estado: campos extras no JSON são recusados.
    /// </summary>
    /// <param name="extraField">Campo que o cliente tenta enviar.</param>
    [TestMethod]
    [DataRow("\"ownerId\":\"outro-usuario\"")]
    [DataRow("\"status\":\"Approved\"")]
    [DataRow("\"createdAtUtc\":\"2026-01-01T00:00:00Z\"")]
    public void Deserialize_ServerControlledField_IsRejected(string extraField)
    {
        string json = "{\"description\":\"Almoço com cliente\",\"amount\":10,\"expenseDate\":\"2026-09-01\"," + extraField + "}";

        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExpenseDraftRequest>(json, JsonSerializerOptions.Web));
    }

    private static ExpenseDraftRequest ValidRequest(
        string description = "Almoço com cliente",
        decimal? amount = 42.90m,
        DateOnly? expenseDate = null,
        int? categoryId = 2) => new()
        {
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate ?? new DateOnly(2026, 9, 1),
            CategoryId = categoryId,
        };

    private static List<ValidationResult> Validate(ExpenseDraftRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(ExpenseDraftRequest request, string member) =>
        Validate(request).Any(result => result.MemberNames.Contains(member));
}
