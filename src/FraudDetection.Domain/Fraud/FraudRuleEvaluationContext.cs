using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Fraud;

/// <summary>
/// Everything a fraud rule needs to make its decision: the transaction under
/// evaluation, a window of that account's recent history, and the current
/// operator-configurable rule settings. Fraud rules never perform I/O themselves —
/// the application layer loads all of this up front and hands it in, keeping the
/// domain layer pure and easy to unit test.
/// </summary>
public sealed class FraudRuleEvaluationContext
{
    public TransactionEvent Transaction { get; }

    /// <summary>
    /// Other transaction events for the same account, ordered most-recent-first,
    /// excluding <see cref="Transaction"/> itself. Callers decide how far back to
    /// look; individual rules decide how much of that window they actually use.
    /// </summary>
    public IReadOnlyList<TransactionEvent> RecentAccountHistory { get; }

    /// <summary>Defaults to <see cref="FraudRuleSettings.Default"/> when not supplied.</summary>
    public FraudRuleSettings Settings { get; }

    public FraudRuleEvaluationContext(
        TransactionEvent transaction,
        IReadOnlyList<TransactionEvent> recentAccountHistory,
        FraudRuleSettings? settings = null)
    {
        Transaction = transaction;
        RecentAccountHistory = recentAccountHistory;
        Settings = settings ?? FraudRuleSettings.Default;
    }
}
