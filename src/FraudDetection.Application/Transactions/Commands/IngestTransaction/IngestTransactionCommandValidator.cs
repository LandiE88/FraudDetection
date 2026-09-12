using FluentValidation;

namespace FraudDetection.Application.Transactions.Commands.IngestTransaction;

public sealed class IngestTransactionCommandValidator : AbstractValidator<IngestTransactionCommand>
{
    public IngestTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.Amount).GreaterThan(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code, e.g. 'ZAR'.");

        RuleFor(x => x.MerchantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.OccurredAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
            .WithMessage("OccurredAtUtc cannot be in the future.");
    }
}
