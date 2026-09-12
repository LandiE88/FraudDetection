namespace FraudDetection.Domain.Fraud;

/// <summary>
/// A single, independent fraud-detection criterion. Implementations must be pure
/// (no I/O, no shared mutable state) so the engine can run them in any order and
/// so they are trivially unit-testable in isolation.
/// </summary>
public interface IFraudRule
{
    /// <summary>
    /// Stable, human-readable identifier for the rule, persisted on any
    /// <see cref="FraudFlag"/> it raises (e.g. "LargeAmount", "HighVelocity").
    /// </summary>
    string Name { get; }

    FraudRuleResult Evaluate(FraudRuleEvaluationContext context);
}
