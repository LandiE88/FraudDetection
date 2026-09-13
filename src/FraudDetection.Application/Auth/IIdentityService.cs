namespace FraudDetection.Application.Auth;

/// <summary>
/// Abstraction over the user store, so the application layer can create accounts and
/// check credentials without depending on ASP.NET Core Identity directly — the same
/// separation every repository interface in this codebase follows. Implemented in
/// Infrastructure against <c>UserManager&lt;ApplicationUser&gt;</c>.
/// </summary>
public interface IIdentityService
{
    Task<CreateUserResult> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Returns the user's id if the email/password pair is valid, otherwise <c>null</c>.</summary>
    Task<Guid?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of registering a new user. <see cref="Errors"/> is empty when <see cref="Succeeded"/> is true.</summary>
public sealed record CreateUserResult(bool Succeeded, Guid UserId, IReadOnlyList<IdentityFieldError> Errors)
{
    public static CreateUserResult Success(Guid userId) => new(true, userId, Array.Empty<IdentityFieldError>());

    public static CreateUserResult Failure(IReadOnlyList<IdentityFieldError> errors) => new(false, Guid.Empty, errors);
}

/// <summary>An identity-store validation failure, attributed to the field it concerns where known.</summary>
public sealed record IdentityFieldError(string Field, string Message);
