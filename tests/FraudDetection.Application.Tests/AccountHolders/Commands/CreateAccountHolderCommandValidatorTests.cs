using FluentValidation.TestHelper;
using FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Commands;

public class CreateAccountHolderCommandValidatorTests
{
    private readonly CreateAccountHolderCommandValidator _validator = new();

    private static CreateAccountHolderCommand ValidCommand() => new(
        "Jane", "Doe", "A1234567", "jane.doe@example.com", new DateOnly(1990, 5, 20));

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankFirstName_HasError(string firstName)
    {
        var command = ValidCommand() with { FirstName = firstName };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData("missing-domain@")]
    public void Validate_WithInvalidEmail_HasError(string email)
    {
        var command = ValidCommand() with { Email = email };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithFutureDateOfBirth_HasError()
    {
        var command = ValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.DateOfBirth);
    }

    [Fact]
    public void Validate_WithIdPassportOverMaxLength_HasError()
    {
        var command = ValidCommand() with { IdPassport = new string('9', 101) };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.IdPassport);
    }
}
