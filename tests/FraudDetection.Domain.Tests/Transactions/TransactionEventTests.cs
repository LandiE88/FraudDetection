using FluentAssertions;
using FraudDetection.Domain.Exceptions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using FraudDetection.Domain.Transactions.Events;
using Xunit;

namespace FraudDetection.Domain.Tests.Transactions;

public class TransactionEventTests
{
    [Fact]
    public void Create_WithEmptyAccountId_Throws()
    {
        var act = () => TransactionEvent.Create(
            Guid.Empty, TransactionCategory.Purchase, Money.Create(10m, "ZAR"), "Merchant",
            DateTime.UtcNow, DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithBlankMerchantName_Throws()
    {
        var act = () => TransactionEvent.Create(
            Guid.NewGuid(), TransactionCategory.Purchase, Money.Create(10m, "ZAR"), "   ",
            DateTime.UtcNow, DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WhenOccurredFarAfterIngested_Throws()
    {
        var ingested = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var occurred = ingested.AddHours(1);

        var act = () => TransactionEvent.Create(
            Guid.NewGuid(), TransactionCategory.Purchase, Money.Create(10m, "ZAR"), "Merchant",
            occurred, ingested);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ApplyFraudAssessment_WithNoTriggeredRules_LeavesTransactionUnflagged()
    {
        var transaction = TransactionEventFactory.Create();

        transaction.ApplyFraudAssessment(new[] { FraudRuleResult.Clear("SomeRule") });

        transaction.IsFlagged.Should().BeFalse();
        transaction.FraudFlags.Should().BeEmpty();
        transaction.HighestSeverity.Should().BeNull();
        transaction.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ApplyFraudAssessment_WithTriggeredRule_AddsFlagAndRaisesDomainEvent()
    {
        var transaction = TransactionEventFactory.Create();

        transaction.ApplyFraudAssessment(new[]
        {
            FraudRuleResult.Clear("ClearRule"),
            FraudRuleResult.Flag("SuspiciousRule", FraudSeverity.High, "looked suspicious")
        });

        transaction.IsFlagged.Should().BeTrue();
        transaction.FraudFlags.Should().ContainSingle(f => f.RuleName == "SuspiciousRule");
        transaction.HighestSeverity.Should().Be(FraudSeverity.High);

        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionFlaggedForFraudEvent>();
    }

    [Fact]
    public void ApplyFraudAssessment_WithMultipleTriggeredRules_HighestSeverityWins()
    {
        var transaction = TransactionEventFactory.Create();

        transaction.ApplyFraudAssessment(new[]
        {
            FraudRuleResult.Flag("RuleA", FraudSeverity.Medium, "reason A"),
            FraudRuleResult.Flag("RuleB", FraudSeverity.Critical, "reason B")
        });

        transaction.FraudFlags.Should().HaveCount(2);
        transaction.HighestSeverity.Should().Be(FraudSeverity.Critical);
    }
}
