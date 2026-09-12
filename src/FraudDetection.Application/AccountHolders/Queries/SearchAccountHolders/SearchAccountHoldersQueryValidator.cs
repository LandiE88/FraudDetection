using FluentValidation;
using FraudDetection.Domain.AccountHolders;

namespace FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;

public sealed class SearchAccountHoldersQueryValidator : AbstractValidator<SearchAccountHoldersQuery>
{
    public SearchAccountHoldersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        // These are substring searches, not exact filters (a partial name, a passport
        // fragment, part of an email domain), so we only sanity-check length here
        // rather than enforcing full format — e.g. EmailAddress's regex would wrongly
        // reject a legitimate partial search like "@gmail.com".
        RuleFor(x => x.FirstName).MaximumLength(AccountHolder.NameMaxLength);
        RuleFor(x => x.LastName).MaximumLength(AccountHolder.NameMaxLength);
        RuleFor(x => x.IdPassport).MaximumLength(AccountHolder.IdPassportMaxLength);
        RuleFor(x => x.Email).MaximumLength(EmailAddress.MaxLength);

        RuleFor(x => x.BirthMonth).InclusiveBetween(1, 12).When(x => x.BirthMonth.HasValue);

        RuleFor(x => x.BirthYear)
            .InclusiveBetween(1900, DateTime.UtcNow.Year)
            .When(x => x.BirthYear.HasValue);

        RuleFor(x => x)
            .Must(HaveAtLeastOneCriterion)
            .WithMessage("At least one search criterion must be provided.")
            .WithName("SearchCriteria");
    }

    private static bool HaveAtLeastOneCriterion(SearchAccountHoldersQuery query) =>
        query.AccountId.HasValue ||
        !string.IsNullOrWhiteSpace(query.FirstName) ||
        !string.IsNullOrWhiteSpace(query.LastName) ||
        !string.IsNullOrWhiteSpace(query.IdPassport) ||
        !string.IsNullOrWhiteSpace(query.Email) ||
        query.BirthYear.HasValue ||
        query.BirthMonth.HasValue;
}
