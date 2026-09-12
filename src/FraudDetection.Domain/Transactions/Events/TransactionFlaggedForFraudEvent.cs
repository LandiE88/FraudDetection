using FraudDetection.Domain.Common;

namespace FraudDetection.Domain.Transactions.Events;

/// <summary>
/// Raised when at least one fraud rule triggers for a transaction event during
/// evaluation. Downstream handlers (alerting, notifications, audit logging) can
/// subscribe without the domain needing to know they exist.
/// </summary>
public sealed class TransactionFlaggedForFraudEvent : IDomainEvent
{
    public Guid TransactionEventId { get; }
    public Guid AccountId { get; }
    public IReadOnlyCollection<string> TriggeredRuleNames { get; }
    public DateTime OccurredOnUtc { get; }

    public TransactionFlaggedForFraudEvent(
        Guid transactionEventId,
        Guid accountId,
        IReadOnlyCollection<string> triggeredRuleNames,
        DateTime occurredOnUtc)
    {
        TransactionEventId = transactionEventId;
        AccountId = accountId;
        TriggeredRuleNames = triggeredRuleNames;
        OccurredOnUtc = occurredOnUtc;
    }
}
