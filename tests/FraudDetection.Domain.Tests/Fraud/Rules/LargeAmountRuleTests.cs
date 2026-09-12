using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud.Rules;

public class LargeAmountRuleTests
{
    private readonly LargeAmountRule _rule = new();

    [Fact]
    public void Evaluate_WithAmountBelowThreshold_DoesNotTrigger()
    {
        var transaction = TransactionEventFactory.Create(category: TransactionCategory.Purchase, amount: 100m);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithAmountJustOverThreshold_TriggersHighSeverity()
    {
        var transaction = TransactionEventFactory.Create(category: TransactionCategory.Purchase, amount: 5_000.01m);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.High);
    }

    [Fact]
    public void Evaluate_WithAmountAtOrOverFiftyThousand_TriggersCriticalSeverity()
    {
        // Critical currently kicks in at a flat R50,000, regardless of category or
        // threshold — see the severity check in LargeAmountRule.Evaluate.
        var transaction = TransactionEventFactory.Create(category: TransactionCategory.Purchase, amount: 50_000m);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.Critical);
    }

    [Theory]
    [InlineData(TransactionCategory.Withdrawal, 2_000)]
    [InlineData(TransactionCategory.Transfer, 10_000)]
    [InlineData(TransactionCategory.Deposit, 20_000)]
    [InlineData(TransactionCategory.Refund, 3_000)]
    public void Evaluate_UsesDifferentThresholdPerCategory(TransactionCategory category, decimal threshold)
    {
        var atThreshold = TransactionEventFactory.Create(category: category, amount: threshold);
        var overThreshold = TransactionEventFactory.Create(category: category, amount: threshold + 1);

        _rule.Evaluate(new FraudRuleEvaluationContext(atThreshold, Array.Empty<TransactionEvent>())).Triggered.Should().BeFalse();
        _rule.Evaluate(new FraudRuleEvaluationContext(overThreshold, Array.Empty<TransactionEvent>())).Triggered.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithCustomThresholdInSettings_OverridesTheBuiltInDefault()
    {
        var settings = new FraudRuleSettings
        {
            LargeAmountThresholds = new Dictionary<TransactionCategory, decimal> { [TransactionCategory.Purchase] = 50m }
        };

        // Well under the built-in R5,000 default, but over the R50 configured here.
        var transaction = TransactionEventFactory.Create(category: TransactionCategory.Purchase, amount: 75m);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>(), settings));

        result.Triggered.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithSettingsMissingACategory_FallsBackToBuiltInDefaultForThatCategory()
    {
        // Settings only override Purchase; Withdrawal should still use DefaultThresholds.
        var settings = new FraudRuleSettings
        {
            LargeAmountThresholds = new Dictionary<TransactionCategory, decimal> { [TransactionCategory.Purchase] = 50m }
        };

        var atDefaultWithdrawalThreshold = TransactionEventFactory.Create(category: TransactionCategory.Withdrawal, amount: 2_000m);
        var overDefaultWithdrawalThreshold = TransactionEventFactory.Create(category: TransactionCategory.Withdrawal, amount: 2_000.01m);

        _rule.Evaluate(new FraudRuleEvaluationContext(atDefaultWithdrawalThreshold, Array.Empty<TransactionEvent>(), settings))
            .Triggered.Should().BeFalse();
        _rule.Evaluate(new FraudRuleEvaluationContext(overDefaultWithdrawalThreshold, Array.Empty<TransactionEvent>(), settings))
            .Triggered.Should().BeTrue();
    }
}
