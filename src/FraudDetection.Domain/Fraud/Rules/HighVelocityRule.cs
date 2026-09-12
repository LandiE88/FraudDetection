namespace FraudDetection.Domain.Fraud.Rules;

/// <summary>
/// Flags an account that is transacting unusually rapidly: too many events in a
/// short rolling window is a classic signal of a compromised account or a bot.
/// </summary>
public sealed class HighVelocityRule : IFraudRule
{
    public string Name => "HighVelocity";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private const int MaxEventsInWindow = 5;

    public FraudRuleResult Evaluate(FraudRuleEvaluationContext context)
    {
        var windowStart = context.Transaction.OccurredAtUtc - Window;

        var eventsInWindow = context.RecentAccountHistory
            .Count(t => t.OccurredAtUtc >= windowStart && t.OccurredAtUtc <= context.Transaction.OccurredAtUtc);

        // +1 to count the transaction being evaluated itself.
        var totalInWindow = eventsInWindow + 1;

        if (totalInWindow <= MaxEventsInWindow)
        {
            return FraudRuleResult.Clear(Name);
        }

        var severity = totalInWindow >= MaxEventsInWindow * 2 ? FraudSeverity.Critical : FraudSeverity.High;

        return FraudRuleResult.Flag(
            Name,
            severity,
            $"Account had {totalInWindow} transactions within a {Window.TotalMinutes:0}-minute window (limit {MaxEventsInWindow}).");
    }
}
