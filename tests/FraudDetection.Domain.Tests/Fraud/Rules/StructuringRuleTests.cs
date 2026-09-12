using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud.Rules;

public class StructuringRuleTests
{
    private readonly StructuringRule _rule = new();

    [Theory]
    [InlineData(9_900)]
    [InlineData(9_999.99)]
    [InlineData(2_950)]
    public void Evaluate_WithAmountJustBelowAThreshold_Triggers(decimal amount)
    {
        var transaction = TransactionEventFactory.Create(amount: amount);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.High);
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(8_000)]
    [InlineData(100)]
    public void Evaluate_WithAmountNotNearAThreshold_DoesNotTrigger(decimal amount)
    {
        var transaction = TransactionEventFactory.Create(amount: amount);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeFalse();
    }
}
