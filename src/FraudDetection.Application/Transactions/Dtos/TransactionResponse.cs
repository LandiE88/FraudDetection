namespace FraudDetection.Application.Transactions.Dtos;

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? AccountHolderId,
    string Category,
    decimal Amount,
    string Currency,
    string MerchantName,
    DateTime OccurredAtUtc,
    DateTime IngestedAtUtc,
    bool IsFlagged,
    string? HighestSeverity,
    IReadOnlyCollection<FraudFlagResponse> FraudFlags);
