namespace FraudDetection.Application.Transactions.Dtos;

public sealed record FraudFlagResponse(
    Guid Id,
    string RuleName,
    string Severity,
    string Reason,
    DateTime FlaggedAtUtc);
