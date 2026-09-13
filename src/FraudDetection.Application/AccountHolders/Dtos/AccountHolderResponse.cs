namespace FraudDetection.Application.AccountHolders.Dtos;

public sealed record AccountHolderResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
