namespace FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;

/// <summary>
/// Replaces an existing account holder's mutable details (name, id/passport, email,
/// date of birth) — not which account they're linked to; see
/// <c>AccountHolder.Update</c>. Handled by <see cref="UpdateAccountHolderCommandHandler"/>.
/// </summary>
public sealed record UpdateAccountHolderCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string IdPassport,
    string Email,
    DateOnly DateOfBirth);
