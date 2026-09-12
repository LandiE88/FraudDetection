using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud.Rules;

public class DuplicateTransactionRuleTests
{
    private readonly DuplicateTransactionRule _rule = new();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_WithNoMatchingHistory_DoesNotTrigger()
    {
        var current = TransactionEventFactory.Create(accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithIdenticalRecentTransaction_Triggers()
    {
        var previous = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now.AddMinutes(-1));
        var current = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, new[] { previous }));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.Medium);
    }

    [Fact]
    public void Evaluate_WithSameAmountDifferentMerchant_DoesNotTrigger()
    {
        var previous = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now.AddMinutes(-1));
        var current = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Gas Station", occurredAtUtc: Now);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, new[] { previous }));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithMatchOutsideWindow_DoesNotTrigger()
    {
        var previous = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now.AddMinutes(-10));
        var current = TransactionEventFactory.Create(
            accountId: AccountId, amount: 50m, merchantName: "Coffee Shop", occurredAtUtc: Now);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, new[] { previous }));

        result.Triggered.Should().BeFalse();
    }
}
