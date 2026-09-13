using FraudDetection.Domain.Transactions;

namespace FraudDetection.Application.Transactions.Commands.IngestTransaction;

/// <summary>
/// Ingests a categorized transaction event, runs the fraud rule engine against it
/// (using the account's recent history) and persists the result. Handled by
/// <see cref="IngestTransactionCommandHandler"/>.
/// </summary>
public sealed record IngestTransactionCommand(
    Guid AccountId,
    TransactionCategory Category,
    decimal Amount,
    string Currency,
    string MerchantName,
    DateTime OccurredAtUtc,
    Guid? AccountHolderId = null);
