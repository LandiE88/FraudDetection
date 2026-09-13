namespace FraudDetection.Application.Auth.Commands.Login;

/// <summary>Exchanges an email/password pair for a JWT access token. Handled by <see cref="LoginCommandHandler"/>.</summary>
public sealed record LoginCommand(string Email, string Password);
