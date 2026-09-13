using FraudDetection.Domain.Transactions;

namespace FraudDetection.Api.Contracts;

/// <summary>Request body for POST /api/transactions.</summary>
public sealed record CreateTransactionRequest(
    Guid AccountId,
    TransactionCategory Category,
    decimal Amount,
    string Currency,
    string MerchantName,
    DateTime OccurredAtUtc,
    Guid? AccountHolderId = null);
