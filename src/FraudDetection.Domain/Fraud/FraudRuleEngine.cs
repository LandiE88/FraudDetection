namespace FraudDetection.Domain.Fraud;

/// <summary>
/// Domain service that runs the full configured set of <see cref="IFraudRule"/>
/// implementations against a transaction and returns every rule's verdict. Stateless
/// and side-effect free — persisting the outcome is the caller's responsibility.
/// </summary>
public sealed class FraudRuleEngine
{
    private readonly IReadOnlyCollection<IFraudRule> _rules;

    public FraudRuleEngine(IEnumerable<IFraudRule> rules)
    {
        _rules = rules.ToList().AsReadOnly();

        if (_rules.Count == 0)
        {
            throw new InvalidOperationException("FraudRuleEngine requires at least one IFraudRule to be registered.");
        }
    }

    public IReadOnlyCollection<FraudRuleResult> Evaluate(FraudRuleEvaluationContext context) =>
        _rules.Select(rule => rule.Evaluate(context)).ToList();
}
