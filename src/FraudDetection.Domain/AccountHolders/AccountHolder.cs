using FraudDetection.Domain.Common;
using FraudDetection.Domain.Exceptions;

namespace FraudDetection.Domain.AccountHolders;

/// <summary>
/// A person who can be linked to transaction events via <see cref="AggregateRoot{TId}.Id"/>
/// — <c>TransactionEvent.AccountHolderId</c> is a real, database-enforced foreign key
/// to this entity's <see cref="AggregateRoot{TId}.Id"/>; that FK is the only relation
/// between the two tables.
/// </summary>
public sealed class AccountHolder : AggregateRoot<Guid>
{
    public const int NameMaxLength = 100;
    public const int IdPassportMaxLength = 100;

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
        string firstName,
        string lastName,
        string idPassport,
        EmailAddress email,
        DateOnly dateOfBirth) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        IdPassport = idPassport;
        Email = email;
        DateOfBirth = dateOfBirth;
    }

    public static AccountHolder Create(
        string firstName,
        string lastName,
        string idPassport,
        EmailAddress email,
        DateOnly dateOfBirth,
        DateOnly today)
    {
        return new AccountHolder(
            Guid.NewGuid(),
            RequireName(firstName, nameof(firstName)),
            RequireName(lastName, nameof(lastName)),
            RequireIdPassport(idPassport),
            email,
            RequireNotFutureDateOfBirth(dateOfBirth, today));
    }

    /// <summary>
    /// Replaces every mutable field (everything but the identity-defining
    /// <see cref="AggregateRoot{TId}.Id"/>). Same invariants as <see cref="Create"/>.
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
