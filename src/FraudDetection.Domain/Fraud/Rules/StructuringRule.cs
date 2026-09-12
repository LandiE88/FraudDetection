namespace FraudDetection.Domain.Fraud.Rules;

/// <summary>
/// Flags amounts that look deliberately chosen to sit just under a common reporting
/// threshold (e.g. 9,000-9,999.99 when 10,000 triggers reporting requirements) — a
/// pattern known as "structuring" or "smurfing".
/// </summary>
public sealed class StructuringRule : IFraudRule
{
    public string Name => "Structuring";

    private static readonly decimal[] ReportingThresholds = { 3_000m, 10_000m };
    private const decimal MarginBelowThreshold = 200m;

    public FraudRuleResult Evaluate(FraudRuleEvaluationContext context)
    {
        var amount = context.Transaction.Amount.Amount;

        foreach (var threshold in ReportingThresholds)
        {
            var lowerBound = threshold - MarginBelowThreshold;

            if (amount >= lowerBound && amount < threshold)
            {
                return FraudRuleResult.Flag(
                    Name,
                    FraudSeverity.High,
                    $"Amount of {amount:0.00} sits just below the {threshold:0.00} reporting threshold, consistent with structuring.");
            }
        }

        return FraudRuleResult.Clear(Name);
    }
}
