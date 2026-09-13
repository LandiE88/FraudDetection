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

        return new AccountHolder(
            Guid.NewGuid(),
            accountId,
            RequireName(firstName, nameof(firstName)),
            RequireName(lastName, nameof(lastName)),
            RequireIdPassport(idPassport),
            email,
            RequireNotFutureDateOfBirth(dateOfBirth, today));
    }

    /// <summary>
    /// Replaces every mutable field (everything but the identity-defining
    /// <see cref="AggregateRoot{TId}.Id"/> and <see cref="AccountId"/> — the latter
    /// is which (holder, account) row this is, not something you "correct" in place;
    /// re-ingest a new one instead if a holder actually needs linking to a different
    /// account). Same invariants as <see cref="Create"/>.
    /// </summary>
    public void Update(
        string firstName,
        string lastName,
        string idPassport,
        EmailAddress email,
        DateOnly dateOfBirth,
        DateOnly today)
    {
        FirstName = RequireName(firstName, nameof(firstName));
        LastName = RequireName(lastName, nameof(lastName));
        IdPassport = RequireIdPassport(idPassport);
        Email = email;
        DateOfBirth = RequireNotFutureDateOfBirth(dateOfBirth, today);
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

    private static string RequireIdPassport(string idPassport)
    {
        if (string.IsNullOrWhiteSpace(idPassport))
        {
            throw new DomainException("Id/passport number is required.");
        }

        var trimmed = idPassport.Trim();

        if (trimmed.Length > IdPassportMaxLength)
        {
            throw new DomainException($"Id/passport number must be at most {IdPassportMaxLength} characters.");
        }

        return trimmed;
    }

    private static DateOnly RequireNotFutureDateOfBirth(DateOnly dateOfBirth, DateOnly today)
    {
        if (dateOfBirth > today)
        {
            throw new DomainException("Date of birth cannot be in the future.");
        }

        return dateOfBirth;
    }
}
