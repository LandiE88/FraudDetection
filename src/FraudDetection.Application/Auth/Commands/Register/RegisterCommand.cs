namespace FraudDetection.Application.Auth.Commands.Register;

/// <summary>Registers a new user account. Handled by <see cref="RegisterCommandHandler"/>.</summary>
public sealed record RegisterCommand(string Email, string Password);
