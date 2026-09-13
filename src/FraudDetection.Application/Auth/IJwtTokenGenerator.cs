namespace FraudDetection.Application.Auth;

/// <summary>
/// Issues signed access tokens for an authenticated user. Implemented in
/// Infrastructure (the only layer allowed to depend on a JWT library), so the
/// application layer only ever deals in the resulting <see cref="GeneratedToken"/>.
/// </summary>
public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(Guid userId, string email);
}

public sealed record GeneratedToken(string Value, DateTime ExpiresAtUtc);
