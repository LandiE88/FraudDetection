using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Tests.TestHelpers;

/// <summary>Convenience builder for <see cref="TransactionEvent"/> instances in tests.</summary>
public static class TransactionEventFactory
{
    public static TransactionEvent Create(
        Guid? accountId = null,
        TransactionCategory category = TransactionCategory.Purchase,
        decimal amount = 100m,
        string currency = "ZAR",
        string merchantName = "Acme Store",
        DateTime? occurredAtUtc = null,
        DateTime? ingestedAtUtc = null)
    {
        var occurred = occurredAtUtc ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        return TransactionEvent.Create(
            accountId ?? Guid.NewGuid(),
            category,
            Money.Create(amount, currency),
            merchantName,
            occurred,
            ingestedAtUtc ?? occurred);
    }
}
