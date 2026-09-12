using FraudDetection.Domain.Transactions;

namespace FraudDetection.Infrastructure.Persistence.FraudRuleSettings;

/// <summary>
/// EF Core row shape for the <c>large_amount_thresholds</c> table: one configurable
/// threshold per <see cref="TransactionCategory"/>. This is plain configuration
/// storage, not a domain entity — it has no identity or behavior of its own, which is
/// why it lives in Infrastructure rather than Domain.
/// </summary>
public sealed class LargeAmountThresholdRecord
{
    public TransactionCategory Category { get; set; }
    public decimal ThresholdAmount { get; set; }
}
