using FluentValidation.TestHelper;
using FraudDetection.Application.Auth.Commands.Register;
using Xunit;

namespace FraudDetection.Application.Tests.Auth.Commands;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() => new("jane.doe@example.com", "Str0ngPass");

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_HasError(string email)
    {
        var command = ValidCommand() with { Email = email };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithPasswordUnderMinimumLength_HasError()
    {
        var command = ValidCommand() with { Password = "Ab1" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Theory]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Validate_WithPasswordMissingACharacterClass_HasError(string password)
    {
        var command = ValidCommand() with { Password = password };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
