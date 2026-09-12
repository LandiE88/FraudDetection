namespace FraudDetection.Application.AccountHolders.Dtos;

public sealed record AccountHolderResponse(
    Guid Id,
    Guid AccountId,
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
