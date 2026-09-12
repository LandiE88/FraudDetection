using FraudDetection.Domain.Common;
using FraudDetection.Domain.Exceptions;

namespace FraudDetection.Domain.AccountHolders;

/// <summary>
/// A person associated with a financial account. One holder can be associated with
/// several accounts and — for joint accounts — one account can have several holders;
/// each (holder, account) pairing is its own row, sharing the same personal details.
///
/// <see cref="AccountId"/> is only <em>logically</em> linked to
/// <c>TransactionEvent.AccountId</c> — not a real foreign key. An account isn't a
/// first-class row anywhere in this system (it's just a grouping id shared by many
/// transaction events), and a foreign key must target a unique/primary key, which
/// <c>transaction_events.AccountId</c> isn't. Both sides are indexed on
/// <c>AccountId</c> so joining/filtering by it stays cheap regardless.
/// </summary>
public sealed class AccountHolder : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int IdPassportMaxLength = 100;

    public Guid AccountId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string IdPassport { get; private set; } = default!;
    public EmailAddress Email { get; private set; } = default!;
    public DateOnly DateOfBirth { get; private set; }

    private AccountHolder()
    {
        // EF Core
    }

    private AccountHolder(
        Guid id,
        Guid accountId,
        string firstName,
        string lastName,
        string idPassport,
        EmailAddress email,
        DateOnly dateOfBirth) : base(id)
    {
        AccountId = accountId;
        FirstName = firstName;
        LastName = lastName;
        IdPassport = idPassport;
        Email = email;
        DateOfBirth = dateOfBirth;
    }

    public static AccountHolder Create(
        Guid accountId,
        string firstName,
        string lastName,
        string idPassport,
        EmailAddress email,
        DateOnly dateOfBirth,
        DateOnly today)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account id is required.");
        }

        firstName = RequireName(firstName, nameof(firstName));
        lastName = RequireName(lastName, nameof(lastName));

        if (string.IsNullOrWhiteSpace(idPassport))
        {
            throw new DomainException("Id/passport number is required.");
        }

        if (idPassport.Trim().Length > IdPassportMaxLength)
        {
            throw new DomainException($"Id/passport number must be at most {IdPassportMaxLength} characters.");
        }

        if (dateOfBirth > today)
        {
            throw new DomainException("Date of birth cannot be in the future.");
        }

        return new AccountHolder(
            Guid.NewGuid(),
            accountId,
            firstName,
            lastName,
            idPassport.Trim(),
            email,
            dateOfBirth);
    }

    private static string RequireName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var trimmed = name.Trim();

        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"{fieldName} must be at most {NameMaxLength} characters.");
        }

        return trimmed;
    }
}
