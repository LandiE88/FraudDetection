namespace FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;

/// <summary>
/// Searches account holders by any combination of name, id/passport number, email,
/// and birth year/month. Handled by <see cref="SearchAccountHoldersQueryHandler"/>.
/// </summary>
public sealed record SearchAccountHoldersQuery(
    string? FirstName,
    string? LastName,
    string? IdPassport,
    string? Email,
    int? BirthYear,
    int? BirthMonth,
    int Page = 1,
    int PageSize = 20);
