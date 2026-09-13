using FluentValidation;
using FraudDetection.Domain.AccountHolders;

namespace FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;

public sealed class UpdateAccountHolderCommandValidator : AbstractValidator<UpdateAccountHolderCommand>
{
    public UpdateAccountHolderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(AccountHolder.NameMaxLength);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(AccountHolder.NameMaxLength);
        RuleFor(x => x.IdPassport).NotEmpty().MaximumLength(AccountHolder.IdPassportMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(EmailAddress.MaxLength)
            .Must(EmailAddress.IsValid)
            .WithMessage("'{PropertyValue}' is not a valid email address.");

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("DateOfBirth cannot be in the future.");
    }
}
