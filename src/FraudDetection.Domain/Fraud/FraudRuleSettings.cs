using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Fraud;

/// <summary>
/// Runtime-configurable knobs for fraud rules. The application layer loads the current
/// values from persistence once per evaluation (the same way it pre-loads account
/// history) and hands them in via <see cref="FraudRuleEvaluationContext"/>, so an
/// operator can retune a rule — e.g. raise or lower a <see cref="LargeAmountRule"/>
/// threshold for a category — without a code change or redeploy. Anything not
/// supplied falls back to <see cref="LargeAmountRule.DefaultThresholds"/>.
/// </summary>
public sealed class FraudRuleSettings
{
    public static FraudRuleSettings Default { get; } = new();

    public IReadOnlyDictionary<TransactionCategory, decimal> LargeAmountThresholds { get; init; } =
        LargeAmountRule.DefaultThresholds;
}
