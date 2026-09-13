namespace FraudDetection.Application.Auth.Dtos;

/// <summary>A signed access token to send back as <c>Authorization: Bearer &lt;token&gt;</c> on subsequent requests.</summary>
public sealed record AuthResponse(string Email, string Token, DateTime ExpiresAtUtc);
