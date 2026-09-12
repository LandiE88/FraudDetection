using FraudDetection.Domain.Common;

namespace FraudDetection.Domain.Fraud;

/// <summary>
/// Records that a single fraud rule was triggered for a transaction. A child entity
/// of the <c>TransactionEvent</c> aggregate — it has no meaning or lifecycle outside it.
/// </summary>
public sealed class FraudFlag : Entity<Guid>
{
    public Guid TransactionEventId { get; private set; }
    public string RuleName { get; private set; } = default!;
    public FraudSeverity Severity { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime FlaggedAtUtc { get; private set; }

    private FraudFlag()
    {
        // EF Core
    }

    internal FraudFlag(Guid transactionEventId, string ruleName, FraudSeverity severity, string reason, DateTime flaggedAtUtc)
        : base(Guid.NewGuid())
    {
        TransactionEventId = transactionEventId;
        RuleName = ruleName;
        Severity = severity;
        Reason = reason;
        FlaggedAtUtc = flaggedAtUtc;
    }
}
