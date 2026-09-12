using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud.Rules;

public class HighVelocityRuleTests
{
    private readonly HighVelocityRule _rule = new();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_WithFewRecentTransactions_DoesNotTrigger()
    {
        var current = TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now);
        var history = BuildHistory(count: 3, minutesApart: 1);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, history));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithManyTransactionsInWindow_Triggers()
    {
        var current = TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now);
        var history = BuildHistory(count: 6, minutesApart: 1);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, history));

        result.Triggered.Should().BeTrue();
        result.Severity.Should().Be(FraudSeverity.High);
    }

    [Fact]
    public void Evaluate_IgnoresHistoryOutsideTheWindow()
    {
        var current = TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now);

        // 10 events, but all well outside the 10-minute window, so they shouldn't count.
        var history = Enumerable.Range(1, 10)
            .Select(i => TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now.AddHours(-2 * i)))
            .ToList();

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, history));

        result.Triggered.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithVeryHighVelocity_TriggersCriticalSeverity()
    {
        var current = TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now);
        var history = BuildHistory(count: 15, minutesApart: 1);

        var result = _rule.Evaluate(new FraudRuleEvaluationContext(current, history));

        result.Severity.Should().Be(FraudSeverity.Critical);
    }

    private static List<TransactionEvent> BuildHistory(int count, int minutesApart) =>
        Enumerable.Range(1, count)
            .Select(i => TransactionEventFactory.Create(accountId: AccountId, occurredAtUtc: Now.AddMinutes(-i * minutesApart)))
            .ToList();
}
