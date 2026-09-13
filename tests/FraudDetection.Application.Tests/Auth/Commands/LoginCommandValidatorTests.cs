using FluentValidation.TestHelper;
using FraudDetection.Application.Auth.Commands.Login;
using Xunit;

namespace FraudDetection.Application.Tests.Auth.Commands;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new LoginCommand("jane.doe@example.com", "whatever")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithBlankEmail_HasError()
    {
        _validator.TestValidate(new LoginCommand("", "whatever")).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithBlankPassword_HasError()
    {
        _validator.TestValidate(new LoginCommand("jane.doe@example.com", "")).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
