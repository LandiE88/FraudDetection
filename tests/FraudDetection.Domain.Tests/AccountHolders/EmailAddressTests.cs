using FluentAssertions;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Exceptions;
using Xunit;

namespace FraudDetection.Domain.Tests.AccountHolders;

public class EmailAddressTests
{
    [Theory]
    [InlineData("jane.doe@example.com")]
    [InlineData("j@example.co.za")]
    [InlineData("first.last+tag@sub.example.com")]
    public void Create_WithValidAddress_Succeeds(string email)
    {
        var result = EmailAddress.Create(email);

        result.Value.Should().Be(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("no-at-sign.example.com")]
    [InlineData("spaces in@example.com")]
    public void Create_WithInvalidAddress_Throws(string email)
    {
        var act = () => EmailAddress.Create(email);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithAddressOverMaxLength_Throws()
    {
        var localPart = new string('a', EmailAddress.MaxLength);
        var tooLong = $"{localPart}@example.com";

        var act = () => EmailAddress.Create(tooLong);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var result = EmailAddress.Create("  jane.doe@example.com  ");

        result.Value.Should().Be("jane.doe@example.com");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        var a = EmailAddress.Create("Jane.Doe@Example.com");
        var b = EmailAddress.Create("jane.doe@example.com");

        a.Should().Be(b);
    }
}
