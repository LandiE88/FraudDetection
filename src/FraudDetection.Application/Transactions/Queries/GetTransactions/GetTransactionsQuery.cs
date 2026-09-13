using FraudDetection.Domain.Transactions;

namespace FraudDetection.Application.Transactions.Queries.GetTransactions;

/// <summary>Searches transactions by filter, paged. Handled by <see cref="GetTransactionsQueryHandler"/>.</summary>
public sealed record GetTransactionsQuery(
    Guid? AccountId,
    Guid? AccountHolderId,
    TransactionCategory? Category,
    bool? OnlyFlagged,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page = 1,
    int PageSize = 20);
