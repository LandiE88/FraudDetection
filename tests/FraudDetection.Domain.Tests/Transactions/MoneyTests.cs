using FluentAssertions;
using FraudDetection.Domain.Exceptions;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.Domain.Tests.Transactions;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        var act = () => Money.Create(amount, "ZAR");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Create_WithInvalidCurrency_Throws(string currency)
    {
        var act = () => Money.Create(100m, currency);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperInvariant()
    {
        var money = Money.Create(100m, "zar");

        money.Currency.Should().Be("ZAR");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Money.Create(50m, "EUR");
        var b = Money.Create(50m, "EUR");
        var c = Money.Create(50.01m, "EUR");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
