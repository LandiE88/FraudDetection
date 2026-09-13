namespace FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;

/// <summary>
/// Registers a new account holder. Handled by
/// <see cref="CreateAccountHolderCommandHandler"/>.
/// </summary>
public sealed record CreateAccountHolderCommand(
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
