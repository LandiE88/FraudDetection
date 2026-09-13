namespace FraudDetection.Infrastructure.Identity;

/// <summary>Bound from the "Jwt" configuration section — see appsettings.json.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key. Must be at least 32 bytes/characters (HMAC-SHA256).
    /// The value checked into appsettings.json is a development-only placeholder —
    /// override it with a real secret via the Jwt__Key environment variable (or a
    /// proper secret store) in any real deployment, the same way the database
    /// connection string is overridden.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpiryMinutes { get; init; } = 60;
}
