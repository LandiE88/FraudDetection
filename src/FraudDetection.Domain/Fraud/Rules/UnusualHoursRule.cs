using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Fraud.Rules;

/// <summary>
/// Flags non-trivial transactions that occur during the dead-of-night UTC window
/// (00:00-05:00), when genuine account-holder activity is statistically rare.
/// </summary>
public sealed class UnusualHoursRule : IFraudRule
{
    public string Name => "UnusualHours";

    private static readonly TimeOnly WindowStart = new(0, 0);
    private static readonly TimeOnly WindowEnd = new(5, 0);
    private const decimal MinimumAmountToFlag = 500m;

    public FraudRuleResult Evaluate(FraudRuleEvaluationContext context)
    {
        var transaction = context.Transaction;
        var timeOfDay = TimeOnly.FromDateTime(transaction.OccurredAtUtc);

        var isDuringUnusualHours = timeOfDay >= WindowStart && timeOfDay < WindowEnd;

        if (!isDuringUnusualHours || transaction.Amount.Amount < MinimumAmountToFlag)
        {
            return FraudRuleResult.Clear(Name);
        }

        return FraudRuleResult.Flag(
            Name,
            FraudSeverity.Medium,
            $"Transaction of {transaction.Amount} occurred at {timeOfDay:HH\\:mm} UTC, inside the unusual-hours window ({WindowStart:HH\\:mm}-{WindowEnd:HH\\:mm} UTC).");
    }
}
