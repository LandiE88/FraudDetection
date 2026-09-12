using FluentAssertions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Tests.TestHelpers;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Fraud;

public class FraudRuleEngineTests
{
    [Fact]
    public void Constructor_WithNoRules_Throws()
    {
        var act = () => new FraudRuleEngine(Array.Empty<IFraudRule>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Evaluate_ReturnsOneResultPerRegisteredRule()
    {
        var engine = new FraudRuleEngine(new IFraudRule[] { new AlwaysClearRule(), new AlwaysFlagRule() });
        var transaction = TransactionEventFactory.Create();

        var results = engine.Evaluate(new FraudRuleEvaluationContext(transaction, Array.Empty<TransactionEvent>()));

        results.Should().HaveCount(2);
        results.Should().Contain(r => r.RuleName == "AlwaysClear" && !r.Triggered);
        results.Should().Contain(r => r.RuleName == "AlwaysFlag" && r.Triggered);
    }

    private sealed class AlwaysClearRule : IFraudRule
    {
        public string Name => "AlwaysClear";
        public FraudRuleResult Evaluate(FraudRuleEvaluationContext context) => FraudRuleResult.Clear(Name);
    }

    private sealed class AlwaysFlagRule : IFraudRule
    {
        public string Name => "AlwaysFlag";
        public FraudRuleResult Evaluate(FraudRuleEvaluationContext context) =>
            FraudRuleResult.Flag(Name, FraudSeverity.Low, "always flags");
    }
}
