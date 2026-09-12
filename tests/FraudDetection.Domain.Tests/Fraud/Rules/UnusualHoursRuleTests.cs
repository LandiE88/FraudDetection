using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud.Rules;

public class UnusualHoursRuleTests
{
    private readonly UnusualHoursRule _rule = new();

    [Fact]
    public void Evaluate_DuringDaytimeHighAmount_DoesNotTrigger()
    {
        var occurredAtUtc = new DateTime(2026, 1, 1, 14, 0, 0, DateTimeKind.Utc);
        var transaction = TransactionEventFactory.Create(amount: 1_000m, occurredAtUtc: occurredAtUtc);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_DuringUnusualHoursWithSmallAmount_DoesNotTrigger()
    {
        var occurredAtUtc = new DateTime(2026, 1, 1, 2, 0, 0, DateTimeKind.Utc);
        var transaction = TransactionEventFactory.Create(amount: 10m, occurredAtUtc: occurredAtUtc);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_DuringUnusualHoursWithMeaningfulAmount_Triggers()
    {
        var occurredAtUtc = new DateTime(2026, 1, 1, 3, 30, 0, DateTimeKind.Utc);
        var transaction = TransactionEventFactory.Create(amount: 750m, occurredAtUtc: occurredAtUtc);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.Medium);
    }

    [Fact]
    public void Evaluate_AtExactWindowBoundary_Triggers()
    {
        var occurredAtUtc = new DateTime(2026, 1, 1, 4, 59, 0, DateTimeKind.Utc);
        var transaction = TransactionEventFactory.Create(amount: 750m, occurredAtUtc: occurredAtUtc);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        result.Triggered.Should().BeTrue();
    }
}
