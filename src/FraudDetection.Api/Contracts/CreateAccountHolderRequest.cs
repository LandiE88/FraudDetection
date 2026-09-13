namespace FraudDetection.Api.Contracts;

/// <summary>Request body for POST /api/account-holders.</summary>
public sealed record CreateAccountHolderRequest(
    Guid AccountId,
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
