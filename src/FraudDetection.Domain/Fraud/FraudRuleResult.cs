using FraudDetection.Domain.Common;

namespace FraudDetection.Domain.Fraud;

/// <summary>
/// The outcome of evaluating a single <see cref="IFraudRule"/> against a transaction.
/// </summary>
public sealed class FraudRuleResult : ValueObject
{
    public string RuleName { get; }
    public bool Triggered { get; }
    public FraudSeverity Severity { get; }
    public string Reason { get; }

    private FraudRuleResult(string ruleName, bool triggered, FraudSeverity severity, string reason)
    {
        RuleName = ruleName;
        Triggered = triggered;
        Severity = severity;
        Reason = reason;
    }

    public static FraudRuleResult Clear(string ruleName) =>
        new(ruleName, triggered: false, FraudSeverity.Low, reason: string.Empty);

    public static FraudRuleResult Flag(string ruleName, FraudSeverity severity, string reason) =>
        new(ruleName, triggered: true, severity, reason);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RuleName;
        yield return Triggered;
        yield return Severity;
        yield return Reason;
    }
}
