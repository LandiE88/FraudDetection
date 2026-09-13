using FluentValidation;

namespace FraudDetection.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    // Mirrors the password policy configured for ASP.NET Core Identity in
    // FraudDetection.Infrastructure/DependencyInjection.cs — kept in sync by hand
    // since the two live in different layers, the same tradeoff EmailAddress.IsValid
    // makes for email format. Failing here means a weak password never reaches the
    // user store at all.
    public const int MinPasswordLength = 8;

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(MinPasswordLength)
            .Matches("[A-Z]").WithMessage("'Password' must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("'Password' must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("'Password' must contain at least one digit.");
    }
}
