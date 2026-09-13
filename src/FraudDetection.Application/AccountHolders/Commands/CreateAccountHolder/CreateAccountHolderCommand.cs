namespace FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;

/// <summary>
/// Registers a person against an account. Handled by
/// <see cref="CreateAccountHolderCommandHandler"/>.
/// </summary>
public sealed record CreateAccountHolderCommand(
    Guid AccountId,
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
