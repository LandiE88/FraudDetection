using System.Text.RegularExpressions;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Exceptions;

namespace FraudDetection.Domain.AccountHolders;

/// <summary>
/// An email address, validated against a regular expression on construction so an
/// <see cref="AccountHolder"/> can never hold a malformed one. Deliberately a simple,
/// linear (non-backtracking) pattern — full RFC 5322 validation regexes are a well
/// known source of catastrophic backtracking (ReDoS) and aren't worth it in practice.
/// </summary>
public sealed class EmailAddress : ValueObject
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    public const int MaxLength = 100;

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email address is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Email address must be at most {MaxLength} characters.");
        }

        if (!Pattern.IsMatch(trimmed))
        {
            throw new DomainException($"'{value}' is not a valid email address.");
        }

        return new EmailAddress(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public override string ToString() => Value;
}
