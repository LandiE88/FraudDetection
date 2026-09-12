using FraudDetection.Domain.Common;
using FraudDetection.Domain.Exceptions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Transactions.Events;

namespace FraudDetection.Domain.Transactions;

/// <summary>
/// A single categorized financial movement submitted to the system, together with
/// whatever fraud flags have been raised against it. This is the aggregate root for
/// the transaction/fraud boundary: <see cref="FraudFlag"/> instances are only ever
/// created and mutated through it.
/// </summary>
public sealed class TransactionEvent : AggregateRoot<Guid>
{
    private readonly List<FraudFlag> _fraudFlags = new();

    public Guid AccountId { get; private set; }
    public TransactionCategory Category { get; private set; }
    public Money Amount { get; private set; } = default!;
    public string MerchantName { get; private set; } = default!;

    /// <summary>When the financial event actually happened, as reported by the caller.</summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>When this system received and recorded the event.</summary>
    public DateTime IngestedAtUtc { get; private set; }

    public IReadOnlyCollection<FraudFlag> FraudFlags => _fraudFlags.AsReadOnly();

    public bool IsFlagged => _fraudFlags.Count > 0;

    public FraudSeverity? HighestSeverity => _fraudFlags.Count == 0 ? null : _fraudFlags.Max(f => f.Severity);

    private TransactionEvent()
    {
        // EF Core
    }

    private TransactionEvent(
        Guid id,
        Guid accountId,
        TransactionCategory category,
        Money amount,
        string merchantName,
        DateTime occurredAtUtc,
        DateTime ingestedAtUtc) : base(id)
    {
        AccountId = accountId;
        Category = category;
        Amount = amount;
        MerchantName = merchantName;
        OccurredAtUtc = occurredAtUtc;
        IngestedAtUtc = ingestedAtUtc;
    }

    public static TransactionEvent Create(
        Guid accountId,
        TransactionCategory category,
        Money amount,
        string merchantName,
        DateTime occurredAtUtc,
        DateTime ingestedAtUtc)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account id is required.");
        }

        if (string.IsNullOrWhiteSpace(merchantName))
        {
            throw new DomainException("Merchant name is required.");
        }

        if (occurredAtUtc > ingestedAtUtc.AddMinutes(5))
        {
            throw new DomainException("A transaction cannot be reported as occurring in the future.");
        }

        return new TransactionEvent(
            Guid.NewGuid(),
            accountId,
            category,
            amount,
            merchantName.Trim(),
            occurredAtUtc,
            ingestedAtUtc);
    }

    /// <summary>
    /// Applies the outcome of running the fraud rule engine against this transaction,
    /// recording a <see cref="FraudFlag"/> for every rule that triggered and raising
    /// <see cref="TransactionFlaggedForFraudEvent"/> if at least one did.
    /// </summary>
    public void ApplyFraudAssessment(IEnumerable<FraudRuleResult> ruleResults)
    {
        var triggered = ruleResults.Where(r => r.Triggered).ToList();

        foreach (var result in triggered)
        {
            _fraudFlags.Add(new FraudFlag(Id, result.RuleName, result.Severity, result.Reason, IngestedAtUtc));
        }

        if (triggered.Count > 0)
        {
            RaiseDomainEvent(new TransactionFlaggedForFraudEvent(
                Id,
                AccountId,
                triggered.Select(r => r.RuleName).ToArray(),
                IngestedAtUtc));
        }
    }
}
