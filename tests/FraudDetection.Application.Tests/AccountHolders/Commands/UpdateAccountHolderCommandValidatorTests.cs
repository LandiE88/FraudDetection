using FluentValidation.TestHelper;
using FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Commands;

public class UpdateAccountHolderCommandValidatorTests
{
    private readonly UpdateAccountHolderCommandValidator _validator = new();

    private static UpdateAccountHolderCommand ValidCommand() => new(
        Guid.NewGuid(), "Jane", "Doe", "A1234567", "jane.doe@example.com", new DateOnly(1990, 5, 20));

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_HasError()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Validate_WithInvalidEmail_HasError()
    {
        var command = ValidCommand() with { Email = "not-an-email" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithFutureDateOfBirth_HasError()
    {
        var command = ValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.DateOfBirth);
    }
}
