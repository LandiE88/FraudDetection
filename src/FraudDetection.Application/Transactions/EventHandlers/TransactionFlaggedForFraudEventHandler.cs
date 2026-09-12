using FraudDetection.Application.Common.Events;
using FraudDetection.Domain.Transactions.Events;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Transactions.EventHandlers;

/// <summary>
/// Reacts to a transaction being flagged for fraud. Currently just logs at a level an
/// alerting pipeline could pick up on; a real system would push this to a
/// notifications/case-management service instead.
/// </summary>
public sealed class TransactionFlaggedForFraudEventHandler : IDomainEventHandler<TransactionFlaggedForFraudEvent>
{
    private readonly ILogger<TransactionFlaggedForFraudEventHandler> _logger;

    public TransactionFlaggedForFraudEventHandler(ILogger<TransactionFlaggedForFraudEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TransactionFlaggedForFraudEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "FRAUD ALERT: transaction {TransactionId} on account {AccountId} triggered rules [{Rules}] at {OccurredOnUtc:O}",
            domainEvent.TransactionEventId,
            domainEvent.AccountId,
            string.Join(", ", domainEvent.TriggeredRuleNames),
            domainEvent.OccurredOnUtc);

        return Task.CompletedTask;
    }
}
