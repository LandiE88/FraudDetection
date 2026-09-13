namespace FraudDetection.Api.Contracts;

/// <summary>Request body for POST /api/auth/register.</summary>
public sealed record RegisterRequest(string Email, string Password);
