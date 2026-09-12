using FluentAssertions;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Exceptions;
using Xunit;

namespace FraudDetection.Domain.Tests.AccountHolders;

public class AccountHolderTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly EmailAddress ValidEmail = EmailAddress.Create("jane.doe@example.com");

    private static AccountHolder CreateValid(
        Guid? accountId = null,
        string firstName = "Jane",
        string lastName = "Doe",
        string idPassport = "A1234567",
        EmailAddress? email = null,
        DateOnly? dateOfBirth = null) =>
        AccountHolder.Create(
            accountId ?? Guid.NewGuid(),
            firstName,
            lastName,
            idPassport,
            email ?? ValidEmail,
            dateOfBirth ?? new DateOnly(1990, 5, 20),
            Today);

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var accountId = Guid.NewGuid();

        var holder = CreateValid(accountId: accountId);

        holder.Id.Should().NotBe(Guid.Empty);
        holder.AccountId.Should().Be(accountId);
        holder.FirstName.Should().Be("Jane");
        holder.LastName.Should().Be("Doe");
        holder.IdPassport.Should().Be("A1234567");
        holder.Email.Should().Be(ValidEmail);
        holder.DateOfBirth.Should().Be(new DateOnly(1990, 5, 20));
    }

    [Fact]
    public void Create_WithEmptyAccountId_Throws()
    {
        var act = () => CreateValid(accountId: Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFirstName_Throws(string firstName)
    {
        var act = () => CreateValid(firstName: firstName);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLastName_Throws(string lastName)
    {
        var act = () => CreateValid(lastName: lastName);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNameOverMaxLength_Throws()
    {
        var tooLong = new string('a', AccountHolder.NameMaxLength + 1);

        var act = () => CreateValid(firstName: tooLong);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankIdPassport_Throws(string idPassport)
    {
        var act = () => CreateValid(idPassport: idPassport);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithIdPassportOverMaxLength_Throws()
    {
        var tooLong = new string('9', AccountHolder.IdPassportMaxLength + 1);

        var act = () => CreateValid(idPassport: tooLong);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithDateOfBirthInTheFuture_Throws()
    {
        var act = () => CreateValid(dateOfBirth: Today.AddDays(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithDateOfBirthEqualToToday_Succeeds()
    {
        var holder = CreateValid(dateOfBirth: Today);

        holder.DateOfBirth.Should().Be(Today);
    }

    [Fact]
    public void Create_TrimsNameAndIdPassport()
    {
        var holder = CreateValid(firstName: "  Jane  ", lastName: "  Doe  ", idPassport: "  A1234567  ");

        holder.FirstName.Should().Be("Jane");
        holder.LastName.Should().Be("Doe");
        holder.IdPassport.Should().Be("A1234567");
    }
}
