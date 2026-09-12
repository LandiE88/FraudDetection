using FluentAssertions;
using FluentValidation.TestHelper;
using FraudDetection.Application.Transactions.Commands.IngestTransaction;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.Commands;

public class IngestTransactionCommandValidatorTests
{
    private readonly IngestTransactionCommandValidator _validator = new();

    private static IngestTransactionCommand ValidCommand() => new(
        Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", DateTime.UtcNow);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyAccountId_HasError()
    {
        var command = ValidCommand() with { AccountId = Guid.Empty };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.AccountId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveAmount_HasError(decimal amount)
    {
        var command = ValidCommand() with { Amount = amount };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLARS")]
    public void Validate_WithInvalidCurrency_HasError(string currency)
    {
        var command = ValidCommand() with { Currency = currency };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Validate_WithBlankMerchantName_HasError()
    {
        var command = ValidCommand() with { MerchantName = " " };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.MerchantName);
    }

    [Fact]
    public void Validate_WithFutureOccurredAtUtc_HasError()
    {
        var command = ValidCommand() with { OccurredAtUtc = DateTime.UtcNow.AddDays(1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.OccurredAtUtc);
    }
}
