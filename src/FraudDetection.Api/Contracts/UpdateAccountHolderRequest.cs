namespace FraudDetection.Api.Contracts;

/// <summary>Request body for PUT /api/account-holders/{id}. Does not include AccountId — see AccountHolder.Update.</summary>
public sealed record UpdateAccountHolderRequest(
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
