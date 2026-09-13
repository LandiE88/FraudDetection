namespace FraudDetection.Domain.AccountHolders;

/// <summary>
/// Search criteria for account holders. String fields match case-insensitively and
/// as a substring (a "search", not an exact filter); the birth year/month are exact
/// matches. Every field is optional and combinable.
/// </summary>
public sealed class AccountHolderQuery
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? IdPassport { get; init; }
    public string? Email { get; init; }
    public int? BirthYear { get; init; }
    public int? BirthMonth { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
